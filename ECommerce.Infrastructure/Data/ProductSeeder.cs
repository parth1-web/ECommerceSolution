
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
            "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSK7AwkcPTo3wB8AtLdWXjt20fE1xnyj54hO_qFRIL53g&s",

        Ingredients =
            "Dalle Khursani chilli, mustard oil, garlic, ginger, turmeric, Himalayan salt, fenugreek, Timur and traditional spices.",

        BestPairings =
            "Excellent with Dal Bhat, Chiura, Sel Roti, Momos, Sekuwa and parathas.",

        StorageAndShelfLife =
            "Store in a cool, dry place. Use a clean, dry spoon. Refrigerate after opening. Best consumed within 6 months of opening.",

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

        Ingredients =
            "Fermented Gundruk, mustard oil, green chilli, garlic, ginger, turmeric, Himalayan salt, fenugreek and traditional spices.",

        BestPairings =
            "Pairs especially well with Dal Bhat, rice, Chiura, Dhido and traditional Nepali meals.",

        StorageAndShelfLife =
            "Store in a cool, dry place. Keep tightly sealed and use a clean, dry spoon. Refrigerate after opening. Best consumed within 6 months.",

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

        Ingredients =
            "Timur pepper, mustard oil, red chilli, garlic, ginger, turmeric, Himalayan salt, fenugreek and mustard seeds.",

        BestPairings =
            "Perfect with Dal Bhat, Momos, Sekuwa, grilled foods, Chiura and fried snacks.",

        StorageAndShelfLife =
            "Store in a cool and dry place away from moisture. Use a clean, dry spoon. Refrigerate after opening. Best consumed within 6 months.",

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
            "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTTLJkXJHI4_IT7HcMZYO0EEu7x5QTtqBBqQyc6seN3eQ&s=10",

        Ingredients =
            "Fresh lemon, mustard oil, turmeric, Himalayan salt, fenugreek, Timur and mustard seeds.",

        BestPairings =
            "Excellent with Dal Bhat, rice, Chiura, parathas, Sel Roti and fried snacks.",

        StorageAndShelfLife =
            "Store in a cool, dry place and keep the jar tightly sealed. Use a clean, dry spoon. Refrigerate after opening. Best consumed within 6 months.",

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
            "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcRtJDvMGmVzmQ0XVb-JVZSowz-9jeAxEhnhLeOlhnXXWg&s",

        Ingredients =
            "Fresh garlic cloves, mustard oil, red chilli, turmeric, Himalayan salt, fenugreek, Timur and mustard seeds.",

        BestPairings =
            "Great with Dal Bhat, Momos, Sekuwa, Chiura, parathas and grilled dishes.",

        StorageAndShelfLife =
            "Store in a cool, dry place. Keep the garlic covered with oil and use a clean, dry spoon. Refrigerate after opening. Best consumed within 6 months.",

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

        Ingredients =
            "Fresh chilli, fresh garlic, mustard oil, turmeric, Himalayan salt, fenugreek, Timur, mustard seeds and traditional spices.",

        BestPairings =
            "Perfect for Momos, Sekuwa, fried snacks, noodles, rice, Dal Bhat and grilled foods.",

        StorageAndShelfLife =
            "Store in a cool, dry place. Use a clean, dry spoon and keep the jar tightly sealed. Refrigerate after opening. Best consumed within 6 months.",

        CategoryId = category.Id,

        IsActive = true,

        CreatedAt = DateTime.UtcNow
    },

    new Product
    {
        Name = "Buffalo Meat Achar",

        Description =
            "Traditional Nepali buffalo meat pickle prepared with tender buffalo meat, mustard oil, chilli, garlic, ginger, turmeric and aromatic Himalayan spices.",

        Price = 850.00m,

        Stock = 35,

        ImageUrl =
            "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcT0FOWNKs3_kyvITDNIr9oGoNDLXURGhTHQV_jF6qSTzQ&s=10",

        Ingredients =
            "Buffalo meat, mustard oil, garlic, ginger, red chilli, turmeric, Himalayan salt, Timur, fenugreek and cumin.",

        BestPairings =
            "Excellent with Chiura, Dal Bhat, beaten rice, Sekuwa-style meals, roti and traditional Nepali snacks.",

        StorageAndShelfLife =
            "Keep refrigerated after opening. Always use a clean, dry spoon. Keep tightly sealed. Follow the storage instructions on the product label.",

        CategoryId = category.Id,

        IsActive = true,

        CreatedAt = DateTime.UtcNow
    },

    new Product
    {
        Name = "Chicken Achar",

        Description =
            "Delicious Nepali-style chicken pickle made with tender chicken pieces, mustard oil, chilli, garlic, ginger and traditional spices.",

        Price = 750.00m,

        Stock = 40,

        ImageUrl =
            "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSDArE5Bu_4owY10GHRO-UEenIPVs5HRd5hiCBSEc2fMw&s=10",

        Ingredients =
            "Chicken, mustard oil, garlic, ginger, red chilli, turmeric, Himalayan salt, Timur, fenugreek and cumin.",

        BestPairings =
            "Pairs well with Chiura, Dal Bhat, rice, roti, Momos and traditional Nepali snacks.",

        StorageAndShelfLife =
            "Refrigerate after opening and keep tightly sealed. Use a clean, dry spoon. Follow the storage instructions on the product label.",

        CategoryId = category.Id,

        IsActive = true,

        CreatedAt = DateTime.UtcNow
    },

    new Product
    {
        Name = "Pork Achar",

        Description =
            "Rich and flavorful pork pickle prepared with tender pork, mustard oil, dried chilli, garlic, ginger and traditional Nepali spices.",

        Price = 800.00m,

        Stock = 35,

        ImageUrl =
            "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQ8L9z4WS3B4T9Qnm3SLO8mrIU4XH0WMPBlMDgagVCs6g&s=10",

        Ingredients =
            "Pork, mustard oil, garlic, ginger, dried chilli, turmeric, Himalayan salt, Timur, fenugreek and cumin.",

        BestPairings =
            "Ideal with Chiura, rice, Dal Bhat, roti and traditional Nepali snacks.",

        StorageAndShelfLife =
            "Refrigerate after opening. Keep tightly sealed and use a clean, dry spoon. Follow the storage instructions on the product label.",

        CategoryId = category.Id,

        IsActive = true,

        CreatedAt = DateTime.UtcNow
    },

    new Product
    {
        Name = "Mutton Achar",

        Description =
            "Traditional mutton pickle made with tender mutton pieces, mustard oil, chilli, garlic, ginger and aromatic Nepali spices.",

        Price = 1000.00m,

        Stock = 30,

        ImageUrl =
            "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcS-Pb_OYMYEW6ouqnnItTjx4314rY6_s5i5kt2xSDl5Ww&s=10",

        Ingredients =
            "Mutton, mustard oil, garlic, ginger, red chilli, turmeric, Himalayan salt, Timur, fenugreek and cumin.",

        BestPairings =
            "Excellent with Chiura, rice, Dal Bhat, roti and traditional Nepali meals.",

        StorageAndShelfLife =
            "Refrigerate after opening. Keep tightly sealed and use a clean, dry spoon. Follow the storage instructions on the product label.",

        CategoryId = category.Id,

        IsActive = true,

        CreatedAt = DateTime.UtcNow
    },

    new Product
    {
        Name = "Buffalo Sukuti Achar",

        Description =
            "Authentic Nepali sukuti pickle made from dried buffalo meat, mustard oil, chilli, garlic, ginger, Timur and traditional spices.",

        Price = 950.00m,

        Stock = 30,

        ImageUrl =
            "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTHFVMUHF5-UFpu1-64WcYZtU3xe_Idznn6m5dnwCjpaw&s=10",

        Ingredients =
            "Dried buffalo meat, mustard oil, garlic, ginger, dried chilli, Timur, turmeric, Himalayan salt and fenugreek.",

        BestPairings =
            "Perfect with Chiura, Dal Bhat, rice, roti and traditional Nepali snacks.",

        StorageAndShelfLife =
            "Keep tightly sealed and refrigerate after opening. Use a clean, dry spoon. Follow the storage instructions on the product label.",

        CategoryId = category.Id,

        IsActive = true,

        CreatedAt = DateTime.UtcNow
    },

    new Product
    {
        Name = "Chicken Sukuti Achar",

        Description =
            "Spicy chicken sukuti pickle prepared from dried chicken, mustard oil, chilli, garlic, ginger, Timur and traditional Nepali spices.",

        Price = 600.00m,

        Stock = 30,

        ImageUrl =
            "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcSUvdEX6ieJDrZ0bjnubOW6kKFrHZiWvcb4EVuncArI-w&s=10",

        Ingredients =
            "Dried chicken, mustard oil, garlic, ginger, dried chilli, Timur, turmeric, Himalayan salt and fenugreek.",

        BestPairings =
            "Excellent with Chiura, Dal Bhat, rice, roti, Momos and traditional snacks.",

        StorageAndShelfLife =
            "Keep tightly sealed and refrigerate after opening. Use a clean, dry spoon. Follow the storage instructions on the product label.",

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
            var existingProduct = await context.Products
                .FirstOrDefaultAsync(p => p.Name == product.Name);

            if (existingProduct == null)
            {
                await context.Products.AddAsync(product);
                continue;
            }

            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.Stock = product.Stock;
            existingProduct.ImageUrl = product.ImageUrl;

            existingProduct.Ingredients = product.Ingredients;
            existingProduct.BestPairings = product.BestPairings;
            existingProduct.StorageAndShelfLife = product.StorageAndShelfLife;

            existingProduct.CategoryId = product.CategoryId;
            existingProduct.IsActive = product.IsActive;
            existingProduct.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        // ==========================================================
        // SAVE PRODUCTS
        // ==========================================================
    }
}

