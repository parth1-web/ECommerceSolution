
using ECommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Infrastructure.Data;

public static class ProductSeeder
{
    private const string PicklesCategoryName = "Pickles";

    public static async Task SeedAsync(AppDbContext context)
    {
        // ==========================================================
        // FIND EXISTING PICKLES CATEGORY
        // ==========================================================

        var category = await context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c =>
                c.Name == PicklesCategoryName);

        if (category == null)
        {
            throw new InvalidOperationException(
                $"Required category '{PicklesCategoryName}' was not found. " +
                "Please make sure the Pickles category is seeded before ProductSeeder runs.");
        }

        // ==========================================================
        // NEW PICKLE PRODUCTS
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
                    "https://images.unsplash.com/photo-1601050690597-df0568f70950",
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
                    "https://images.unsplash.com/photo-1547592180-85f173990554",
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
                    "https://images.unsplash.com/photo-1596040033229-a9821ebd058d",
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
                    "https://images.unsplash.com/photo-1590502593747-42a996133562",
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
                    "https://images.unsplash.com/photo-1540148426945-6cf22a6b2383",
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
                    "https://images.unsplash.com/photo-1601050690597-df0568f70950",
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
                .AnyAsync(p => p.Name == product.Name);

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

