
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Data;

public static class ProductSeeder
{
    private const string PicklesCategoryName = "Pickles";

    public static async Task SeedAsync(AppDbContext context)
    {
        // ==========================================================
        // ENSURE PICKLES CATEGORY EXISTS
        // ==========================================================

        var category = await context.Categories
            .FirstOrDefaultAsync(c =>
                c.Name == PicklesCategoryName);

        if (category == null)
        {
            category = new Category
            {
                Name = PicklesCategoryName,

                Description =
                    "Authentic handcrafted Nepali pickles prepared with traditional spices and natural ingredients.",

                IsActive = true,

                CreatedAt = DateTime.UtcNow
            };

            try
            {
                await context.Categories.AddAsync(category);
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Another initialization/test process may have
                // created the Pickles category at the same time.
                // Reload it instead of failing with a duplicate key.

                context.Entry(category).State = EntityState.Detached;

                category = await context.Categories
                    .FirstOrDefaultAsync(c =>
                        c.Name == PicklesCategoryName);

                if (category == null)
                {
                    throw;
                }
            }
        }

        // ==========================================================
        // PICKLE PRODUCTS
        // ==========================================================

        var products = new List<Product>
        {
            new Product
            {
                Name = "Dalle Khursani Achar",

                Description =
                    "A fiery Nepali chilli pickle made from fresh Dalle Khursani peppers, mustard oil, garlic, turmeric, salt and traditional spices.",

                Price = 350.00m,

                Stock = 50,

                ImageUrl =
                    "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQCwiTWkCSZVOR8h1umWz6GV-MAkVBuQrzoFyOhmvfQJg&s=10",

                CategoryId = category.Id,

                IsActive = true,

                CreatedAt = DateTime.UtcNow
            },

            new Product
            {
                Name = "Gundruk Achar",

                Description =
                    "Traditional Nepali Gundruk pickle prepared from fermented leafy greens, mustard oil, garlic, chilli and authentic Himalayan spices.",

                Price = 300.00m,

                Stock = 45,

                ImageUrl =
                    "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQi39nNQyCeGMQUSLQecuMIhEJHrairteaMOKD453FXEQ&s=10",

                CategoryId = category.Id,

                IsActive = true,

                CreatedAt = DateTime.UtcNow
            },

            new Product
            {
                Name = "Timur Achar",

                Description =
                    "Aromatic Nepali pickle infused with Himalayan Timur pepper, mustard oil, chilli, garlic and traditional spices.",

                Price = 325.00m,

                Stock = 40,

                ImageUrl =
                    "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSVgl6XXCKfkhTWn_V6PstTCmjtN5Z9pEPYUhOQlMY0ZA&s=10",

                CategoryId = category.Id,

                IsActive = true,

                CreatedAt = DateTime.UtcNow
            },

            new Product
            {
                Name = "Lemon Achar",

                Description =
                    "Tangy sun-cured lemon pickle prepared with mustard oil, Himalayan salt, chilli, turmeric and traditional Nepali spices.",

                Price = 280.00m,

                Stock = 60,

                ImageUrl =
                    "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRzxWPUmom__wXm71SjdDCp-eqn6GJlDHJ3BaeyN99-Iw&s=10",

                CategoryId = category.Id,

                IsActive = true,

                CreatedAt = DateTime.UtcNow
            },

            new Product
            {
                Name = "Garlic Achar",

                Description =
                    "Bold and aromatic garlic pickle made with fresh garlic cloves, mustard oil, chilli, turmeric, fenugreek and Himalayan spices.",

                Price = 320.00m,

                Stock = 50,

                ImageUrl =
                    "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRcBmHbNlSPfrvGEXhrLnVnSC8LG0GESeCYEMgoPcT-Ag&s",

                CategoryId = category.Id,

                IsActive = true,

                CreatedAt = DateTime.UtcNow
            },

            new Product
            {
                Name = "Chilli Garlic Achar",

                Description =
                    "A spicy and aromatic combination of fresh chillies and garlic blended with mustard oil, turmeric, salt and traditional Nepali spices.",

                Price = 340.00m,

                Stock = 45,

                ImageUrl =
                    "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcS_LbxqioAb7Hn9Q3fwNm1cdPSzu6nUuS3ZhrU1VRu0fQ&s=10",

                CategoryId = category.Id,

                IsActive = true,

                CreatedAt = DateTime.UtcNow
            }
        };

        // ==========================================================
        // INSERT ONLY MISSING PRODUCTS
        // ==========================================================

        foreach (var product in products)
        {
            var exists = await context.Products
                .AnyAsync(p =>
                    p.Name == product.Name);

            if (exists)
            {
                continue;
            }

            await context.Products.AddAsync(product);
        }

        // ==========================================================
        // SAVE PRODUCTS
        // ==========================================================

        await context.SaveChangesAsync();
    }
}

