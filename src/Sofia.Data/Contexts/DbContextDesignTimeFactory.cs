using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Sofia.Data.Contexts
{

    public class DbContextDesignTimeFactory : IDesignTimeDbContextFactory<SofiaDbContext>
    {
        public DbContextDesignTimeFactory()
        {

        }

        public SofiaDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SofiaDbContext>();
            optionsBuilder.UseSqlServer($@"");

            return new SofiaDbContext(optionsBuilder.Options);
        }
    }
}
