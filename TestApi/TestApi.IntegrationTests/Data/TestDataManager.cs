﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TestApi.Common.Builders;
using TestApi.DAL;
using TestApi.DAL.Exceptions;
using TestApi.Domain;
using TestApi.Domain.Enums;

namespace TestApi.IntegrationTests.Data
{
    public class TestDataManager
    {
        private readonly TestContext _context;
        private readonly DbContextOptions _dbContextOptions;

        public TestDataManager(TestContext context, DbContextOptions dbContextOptions)
        {
            _context = context;
            _dbContextOptions = dbContextOptions;
        }

        public async Task<User> SeedUser(UserType userType = UserType.Judge)
        {
            await using var db = new TestApiDbContext(_dbContextOptions);

            const Application application = Application.TestApi;

            var number = await IterateUserNumber(userType, application);

            var user = new UserBuilder(_context.Config.UsernameStem, number)
                .WithUserType(userType)
                .ForApplication(application)
                .BuildUser();

            await db.Users.AddAsync(user);
            await db.SaveChangesAsync();
            return user;
        }

        public async Task<Allocation> SeedAllocation(Guid userId)
        {
            await using var db = new TestApiDbContext(_dbContextOptions);

            var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId);

            if (user == null) throw new UserNotFoundException(userId);

            var allocation = new Allocation(user);
            await db.Allocations.AddAsync(allocation);
            await db.SaveChangesAsync();
            return allocation;
        }

        public async Task<int> IterateUserNumber(UserType userType, Application application)
        {
            await using var db = new TestApiDbContext(_dbContextOptions);

            var users = await db.Users
                .Where(x => x.UserType == userType && x.Application == application)
                .AsNoTracking()
                .ToListAsync();

            if (users.Count.Equals(0)) return 1;

            return users.Select(user => user.Number).ToList().Max() + 1;
        }

        public async Task<Allocation> AllocateUser(Guid userId)
        {
            await using var db = new TestApiDbContext(_dbContextOptions);

            var allocation = await db.Allocations
                .Where(x => x.UserId == userId)
                .AsNoTracking()
                .SingleOrDefaultAsync();

            allocation.Allocate(1);
            db.Allocations.Update(allocation);
            await db.SaveChangesAsync();
            return allocation;
        }

        public async Task<Allocation> UnallocateUser(Guid userId)
        {
            await using var db = new TestApiDbContext(_dbContextOptions);

            var allocation = await db.Allocations
                .Where(x => x.UserId == userId)
                .AsNoTracking()
                .SingleOrDefaultAsync();

            allocation.Unallocate();
            await db.SaveChangesAsync();
            return allocation;
        }

        public async Task DeleteUsers()
        {
            await using var db = new TestApiDbContext(_dbContextOptions);

            var users = await db.Users
                .Where(x => x.Application == Application.TestApi)
                .AsNoTracking()
                .ToListAsync();

            foreach (var user in users)
            {
                db.Remove(user);
                await db.SaveChangesAsync();
            }
        }

        public async Task<Allocation> GetAllocationByUserId(Guid userId)
        {
            await using var db = new TestApiDbContext(_dbContextOptions);

            return await db.Allocations
                .Where(x => x.UserId == userId)
                .AsNoTracking()
                .SingleOrDefaultAsync();
        }

        public async Task<User> GetUserById(Guid userId)
        {
            await using var db = new TestApiDbContext(_dbContextOptions);

            return await db.Users
                .Where(x => x.Id == userId)
                .AsNoTracking()
                .SingleOrDefaultAsync();
        }
    }
}