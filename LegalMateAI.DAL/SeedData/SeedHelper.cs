// LegalMateAI.DAL/SeedData/SeedHelper.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LegalMateAI.DAL.SeedData
{
    public static class SeedHelper
    {
        /// <summary>
        /// Generic method to seed entities with update support
        /// </summary>
        public static async Task SeedWithUpdateAsync<TEntity>(
            DbContext context,
            IEnumerable<TEntity> seedItems,
            Expression<Func<TEntity, object>> keySelector,
            ILogger logger,
            string entityName,
            Action<TEntity, TEntity>? updateAction = null) where TEntity : class
        {
            var existingItems = await context.Set<TEntity>().ToListAsync();
            var keyFunc = keySelector.Compile();
            
            foreach (var seedItem in seedItems)
            {
                var seedKey = keyFunc(seedItem);
                var existingItem = existingItems.FirstOrDefault(e => 
                    Equals(keyFunc(e), seedKey));
                
                if (existingItem == null)
                {
                    // Add new item
                    logger.LogInformation("Adding new {Entity}: {Key}", entityName, seedKey);
                    await context.Set<TEntity>().AddAsync(seedItem);
                }
                else
                {
                    // Update existing item
                    bool needsUpdate = false;
                    updateAction?.Invoke(existingItem, seedItem);
                    
                    // You can add custom update logic here
                    // For now, we'll let the specific updateAction handle it
                    
                    if (needsUpdate)
                    {
                        context.Set<TEntity>().Update(existingItem);
                        logger.LogInformation("Updated {Entity}: {Key}", entityName, seedKey);
                    }
                }
            }
            
            await context.SaveChangesAsync();
            logger.LogInformation("{Entity} seeding completed. Total count: {Count}", 
                entityName, await context.Set<TEntity>().CountAsync());
        }
    }
}