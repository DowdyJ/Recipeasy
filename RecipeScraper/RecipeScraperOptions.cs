
namespace Recipeasy.RecipeScraper;

[Flags]
public enum RecipeScraperOptions 
{
    None=0x0,
    Ingredients=0x1,
    Instructions=0x2,
    Comments=0x4,
    Blog=0x8,
    NutritionFacts=0x10,
    MetaData=0x20,
    AllContents=0xFF,
    IncludePictures=0x100,
    Metric = 0x200,
}
