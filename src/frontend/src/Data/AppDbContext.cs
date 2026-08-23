using src.Models;
using Microsoft.EntityFrameworkCore;

namespace src.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Presupuesto> Presupuestos => Set<Presupuesto>();

    public DbSet<Proyecto> Proyectos => Set<Proyecto>();

    public DbSet<Contratacion> Contrataciones => Set<Contratacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Presupuesto>(entity =>
        {
            entity.ToTable("presupuestos");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id");

            entity.Property(x => x.Anio)
                .HasColumnName("anio");

            entity.Property(x => x.Mes)
                .HasColumnName("mes");

            entity.Property(x => x.Entidad)
                .HasColumnName("entidad")
                .HasMaxLength(250);

            entity.Property(x => x.Categoria)
                .HasColumnName("categoria")
                .HasMaxLength(250);

            entity.Property(x => x.PresupuestoInicial)
                .HasColumnName("presupuesto_inicial")
                .HasColumnType("numeric(18,2)");

            entity.Property(x => x.PresupuestoModificado)
                .HasColumnName("presupuesto_modificado")
                .HasColumnType("numeric(18,2)");

            entity.Property(x => x.Ejecutado)
                .HasColumnName("ejecutado")
                .HasColumnType("numeric(18,2)");
        });

        modelBuilder.Entity<Proyecto>(entity =>
        {
            entity.ToTable("proyectos");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id");

            entity.Property(x => x.Nombre)
                .HasColumnName("nombre")
                .HasMaxLength(300);

            entity.Property(x => x.Categoria)
                .HasColumnName("categoria")
                .HasMaxLength(250);

            entity.Property(x => x.Presupuesto)
                .HasColumnName("presupuesto")
                .HasColumnType("numeric(18,2)");

            entity.Property(x => x.Ejecutado)
                .HasColumnName("ejecutado")
                .HasColumnType("numeric(18,2)");
        });

        modelBuilder.Entity<Contratacion>(entity =>
        {
            entity.ToTable("contrataciones");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.Id)
                .HasColumnName("id");

            entity.Property(x => x.Ocid)
                .HasColumnName("ocid")
                .HasMaxLength(200);

            entity.Property(x => x.Entidad)
                .HasColumnName("entidad")
                .HasMaxLength(300);

            entity.Property(x => x.Empresa)
                .HasColumnName("empresa")
                .HasMaxLength(300);

            entity.Property(x => x.Monto)
                .HasColumnName("monto")
                .HasColumnType("numeric(18,2)");

            entity.Property(x => x.Fecha)
                .HasColumnName("fecha");
        });
    }
}