# 운영 복구 런북

> DEP 9단계. Claude 작성(2026-11-13), 결정은 사용자. **아직 실행해 본 적 없는 절차다**(결정 B — 운영 DB 덮어쓰기는 리허설하지 않음).
> 처음 실제로 쓰는 날, 걸린 시간과 막힌 곳을 맨 아래 "실행 기록"에 남긴다.

## 0. 어느 절을 여나

| 상황 | 절 | 꺼내는 사본 |
|---|---|---|
| 설정·코드 배포가 잘못됨 | 이 문서 아님 — 이전 sha로 재배포(자동 롤백, dep-plan "운영 롤백 실측") | git · ECR 이미지 |
| **서버는 멀쩡한데 데이터가 망가짐**(잘못된 DELETE, 잘못된 마이그레이션) | **§A DB 교체** | pg_dump(S3 `daily/` 7일, `weekly/` 28일) |
| **디스크·인스턴스를 잃음**(실수로 종료, 볼륨 손상) | **§B 서버 복구** → 필요하면 §A | EBS 스냅샷(DLM, 최근 7개) |

공통 원칙
- 쓰기 권한 작업은 사람이 `nook-prod` 프로필로 한다. 첫 줄에 **계정·리전**(`nook-prod`, `ap-northeast-2`) 확인
- Session Manager 터미널은 긴 줄을 붙이면 줄이 깨진다 → 아래 명령은 짧게 나눠 두었다
- 덤프·스냅샷·옛 DB는 **개인정보 사본**이다. 끝나면 지우고 기록한다(§A-9)

---

## §A DB 교체 (서버는 멀쩡함)

결정(사용자, 2026-11-13): 쓰기 막기 = **api 컨테이너 정지** · 교체 = **새 DB에 복원 후 이름 바꾸기** · 서버에 없는 덤프 = **미리 서명된 URL**

**감수하는 손실**: 고른 덤프 시각 이후의 쓰기는 모두 사라진다(RPO 최대 24시간). 옛 DB는 `nook_old`로 남으므로 필요한 행은 나중에 손으로 옮길 수 있다.

흐름: 1~4는 **서비스를 켠 채로** 한다 → 멈추는 시간은 5~7(이름 바꾸기 + 마이그레이션)뿐이다.

### A-1. 덤프 고르기

```
# 로컬 PC, 읽기 프로필
aws s3api list-objects-v2 --profile nook-readonly --region ap-northeast-2 \
  --bucket nook-prod-backups-<계정ID>-ap-northeast-2-an --prefix daily/ \
  --query 'Contents[].[Key,Size,LastModified]' --output table
```
- 사고가 난 시각 **이전**의 가장 최근 덤프. 크기가 이전 덤프들과 크게 다르면(0, 수백 바이트) 건너뛴다
- 서버에 이미 있는지 확인(최근 3개): `ls -l /opt/nook/backups/` → 있으면 A-2를 건너뛴다

### A-2. 서버로 가져오기 (서버에 없을 때만)

서버 역할에는 S3 읽기 권한이 없다(침해돼도 과거 백업을 못 읽게). 사람이 짧은 URL을 만들어 넘긴다.

```
# 로컬 PC — URL은 10분 동안 누구나 덤프를 받을 수 있는 비밀값. 대화·문서·채팅에 붙이지 않는다
aws s3 presign s3://nook-prod-backups-<계정ID>-ap-northeast-2-an/daily/<파일> \
  --expires-in 600 --profile nook-prod --region ap-northeast-2
```
```
# 서버 — read -rs: 화면·셸 기록에 URL이 남지 않게 입력받는다
read -rs URL
curl -fsS -o /opt/nook/backups/restore.dump "$URL"
unset URL
ls -l /opt/nook/backups/restore.dump
```

### A-3. 새 DB에 복원 (서비스는 켜진 채)

```
cd /opt/nook/backups
docker exec nook-deploy-db-1 sh -c 'createdb -U "$POSTGRES_USER" nook_restore'
docker exec -i nook-deploy-db-1 sh -c 'pg_restore -U "$POSTGRES_USER" -d nook_restore --exit-on-error' < <덤프 파일>
```
- 실패하면 `nook_restore`를 지우고(`dropdb`) 다른 덤프로 다시. 운영 DB `nook`은 아직 손대지 않았다

### A-4. 복원본 검증

```
docker exec -it nook-deploy-db-1 sh -c 'psql -U "$POSTGRES_USER" -d nook_restore'
```
```sql
SELECT count(*) FROM users;  -- reviews, groups 도 같이 본다
SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY 1 DESC LIMIT 1;
\q
```
- 행 수가 사고 전 기대치와 맞는가(사고로 지워진 행이 돌아왔는가)
- 마지막 `MigrationId`를 적어 둔다. 지금 릴리스보다 옛것이면 A-7에서 migrate가 채운다

### A-5. 쓰기 막기 — 여기서부터 서비스 중단

```
sudo systemctl stop nook-backup.timer          # 작업 중 04:00 백업이 끼어들어 이름 바꾸기를 막지 않게
docker stop nook-deploy-api-1                  # restart: unless-stopped → 손으로 멈춘 건 다시 안 뜬다
```
- 화면은 오류가 난다(점검 화면 없음 — 443 공개 뒤 10단계 후보)

### A-6. 이름 바꾸기

```
docker exec -it nook-deploy-db-1 sh -c 'psql -U "$POSTGRES_USER" -d postgres'
```
```sql
SELECT datname, count(*) FROM pg_stat_activity WHERE datname IN ('nook','nook_restore') GROUP BY 1;  -- 0행이어야 한다
ALTER DATABASE nook RENAME TO nook_old;
ALTER DATABASE nook_restore RENAME TO nook;
\q
```
- 연결이 남아 있으면 RENAME이 실패한다 → 누가 붙어 있는지 위 쿼리로 보고 그 컨테이너를 멈춘다
- **되돌리기**: 반대로 두 번 RENAME(`nook` → `nook_restore`, `nook_old` → `nook`)

### A-7. 스키마 맞추고 재개

```
docker start -a nook-deploy-migrate-1          # 마지막 배포 때 만든 migrate 컨테이너를 다시 실행 → 빠진 마이그레이션 적용 후 종료
docker start nook-deploy-api-1
docker run --rm --network nook-deploy_backend curlimages/curl -fsS http://api:8080/health/ready
sudo systemctl start nook-backup.timer
```
- migrate 종료 코드가 0이 아니면 api를 켜지 않는다 → A-6 되돌리기
- migrate 컨테이너가 없으면(`docker ps -a`) GitHub Actions에서 지금 sha를 다시 배포한다 — deploy.sh가 migrate → api → ready를 순서대로 한다
- 확인: 로그인, 사고로 사라졌던 데이터가 화면에 보이는지

### A-8. 사진과의 어긋남

DB와 사진(S3 이미지 버킷)은 따로 복구된다.
- 덤프 **뒤**에 올라온 사진: S3에는 있고 DB엔 없다 → 고아 사진(정리 배치 미구현, 알려진 빈 곳)
- 덤프 **뒤**에 지워진 사진: DB엔 키가 돌아왔는데 S3엔 삭제 마커 → 이전 버전 35일 안이면 삭제 마커를 지워 되살린다

### A-9. 정리 (개인정보 사본)

- 서버 `/opt/nook/backups/restore.dump` 삭제(A-2로 가져온 경우)
- `nook_old`: 옮길 행이 없음을 확인한 뒤 `DROP DATABASE nook_old;` — 늦어도 7일 안. 날짜를 아래 실행 기록에
- 사고 기록은 `incident-log`로

---

## §B 서버 복구 (디스크·인스턴스를 잃음)

DLM 정책 `policy-0f3bef75d79b892ea`가 루트 볼륨을 매일 05:00 KST쯤 찍는다(최근 7개). 스냅샷은 **크래시 일관성**이다 → 서버(OS·Caddy 인증서·릴리스·systemd)는 스냅샷으로, 데이터 정합성은 그 뒤 §A로 맞춘다.

### B-1. 스냅샷 고르기

```
aws ec2 describe-snapshots --owner-ids self --profile nook-readonly --region ap-northeast-2 \
  --query 'sort_by(Snapshots,&StartTime)[].[SnapshotId,StartTime,State,VolumeSize]' --output table
```
사고 이전, `State = completed`인 가장 최근 것.

### B-2a. 인스턴스가 살아 있으면 — 루트 볼륨 교체

인스턴스 ID·탄력적 IP·역할·보안 그룹이 그대로 남는 가장 짧은 길.
```
aws ec2 create-replace-root-volume-task --instance-id i-02eed84198de4f971 \
  --snapshot-id <snap-…> --profile nook-prod --region ap-northeast-2
```
- 인스턴스가 재부팅된다. 새 루트 볼륨이 생기고 옛 볼륨은 분리된 채 남는다

### B-2b. 인스턴스가 없으면 — 스냅샷으로 새 인스턴스

1. 스냅샷 → AMI 등록(`register-image`, arm64, 루트 장치 `/dev/xvda`에 그 스냅샷)
2. 그 AMI로 인스턴스 시작 — 5단계와 같은 설정: t4g.small, `ap-northeast-2b`, 키 페어 없음, IMDSv2 필수·**홉 2**, 역할 `nook-ec2`, SG `nook-web`, 종료 방지 켬
3. 탄력적 IP `eipalloc-050cdcc1f90d554b2`를 새 인스턴스에 연결(A 레코드는 그대로)
4. GitHub 리포 변수의 인스턴스 ID, `deploy/aws/create-dlm.ps1`의 `$instance`를 새 ID로 바꾼다

### B-3. DLM 대상 다시 잡기 ⚠️

정책이 `CopyTags: false`라 **새 볼륨에는 `nook-dlm=daily` 태그가 없다** → 빠뜨리면 DLM이 경고 없이 찍지 않는다.
```
powershell -ExecutionPolicy Bypass -File deploy/aws/create-dlm.ps1   # 인스턴스의 지금 루트 볼륨에 태그를 단다
```
- **옛 볼륨**(B-2a에서 분리된 것, 또는 남은 것)은 태그를 떼거나 삭제한다 — 둘 다 태그가 있으면 둘 다 찍혀 비용이 두 배
- 다음 날 `describe-snapshots`로 새 볼륨 ID의 스냅샷이 생겼는지 확인

### B-4. 서비스 확인 → 데이터

```
docker ps
docker run --rm --network nook-deploy_backend curlimages/curl -fsS http://api:8080/health/ready
systemctl list-timers nook-backup.timer
```
- 서버는 **스냅샷 시각의 상태**다. 그 뒤 배포가 있었다면 GitHub Actions에서 최신 sha를 다시 배포
- 데이터가 스냅샷 시각보다 새로워야 하면(04:00 덤프는 스냅샷보다 약 1시간 앞이라 보통 필요 없음) → §A

---

## 미결 · 측정 안 한 것

- **RTO 미측정**: §A는 DB 부분만 리허설(11-12, 복원 3초 — 데이터가 작음). §B는 B-2a·B-2b 모두 실행한 적 없음 → 서비스 중단이 생기므로 실측 시점은 사용자 결정
- 점검 화면 없음(A-5) — 443 공개 뒤 10단계 후보
- B-2b의 `register-image` 정확한 인자는 처음 쓸 때 확인해 이 문서에 고정

## 실행 기록

| 날짜 | 절 | 사유 | 걸린 시간 | 막힌 곳 · 고칠 것 |
|---|---|---|---|---|
| | | | | |
