using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nook.Domain.Places;

namespace Nook.Infrastructure.Persistence.Configurations;

// erd.md §2 places.
internal sealed class PlaceConfiguration : IEntityTypeConfiguration<Place>
{
    public void Configure(EntityTypeBuilder<Place> builder)
    {
        builder.ToTable("places", table =>
        {
            table.HasCheckConstraint("ck_places_kakao_place_id", "btrim(kakao_place_id) <> ''");
            table.HasCheckConstraint("ck_places_name", "btrim(name) <> ''");
            table.HasCheckConstraint("ck_places_latitude",
                $"latitude >= {Coordinate.MinLatitude} AND latitude <= {Coordinate.MaxLatitude}");
            table.HasCheckConstraint("ck_places_longitude",
                $"longitude >= {Coordinate.MinLongitude} AND longitude <= {Coordinate.MaxLongitude}");
        });

        // 받는 쿼리: erd.md places Q2
        builder.HasKey(p => p.Id).HasName("pk_places");

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(p => p.KakaoPlaceId)
            .HasColumnName("kakao_place_id")
            .HasMaxLength(KakaoPlaceId.MaxLength)
            .HasConversion(k => k.Value, value => KakaoPlaceId.From(value));

        // 받는 쿼리: erd.md places Q1·Q3 (R-PL-01 중복 방지, Q3 ON CONFLICT의 충돌 판정 대상)
        builder.HasIndex(p => p.KakaoPlaceId)
            .IsUnique()
            .HasDatabaseName("ux_places_kakao_place_id");

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(Place.NameMaxLength);

        builder.Property(p => p.Address)
            .HasColumnName("address")
            .HasMaxLength(Place.AddressMaxLength);

        // 값 객체 하나 → 컬럼 두 개. 복원은 Coordinate의 private 생성자(검증 없음 — 범위는 CHECK가 보장)
        builder.ComplexProperty(p => p.Coordinate, coordinate =>
        {
            coordinate.Property(c => c.Latitude).HasColumnName("latitude");
            coordinate.Property(c => c.Longitude).HasColumnName("longitude");
        });
    }
}
