using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sistema_Ventas.Models;
namespace Sistema_Ventas.Data;
public class ContextoDB : IdentityDbContext<Usuario>
{
    public ContextoDB(DbContextOptions<ContextoDB> options)
        : base(options)
    {
    }
}
