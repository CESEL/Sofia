using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Sophia.Data.Contexts
{

    public class DbContextDesignTimeFactory : IDesignTimeDbContextFactory<SophiaDbContext>
    {
        public DbContextDesignTimeFactory()
        {

        }

        public SophiaDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SophiaDbContext>();
            optionsBuilder.UseSqlServer($@"");

            return new SophiaDbContext(optionsBuilder.Options);
        }
    }
}
