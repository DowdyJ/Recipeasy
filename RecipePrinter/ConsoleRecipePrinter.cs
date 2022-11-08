

namespace Recipeasy.RecipePrinter;

public class ConsoleRecipePrinter : IRecipePrinter
{
    public string PrintRecipe(Recipe recipe)
    {
        Console.WriteLine("******************************");

        Console.WriteLine(recipe.Title);

        Console.WriteLine("------------------------------");

        foreach (Recipe.IngredientGroup ig in recipe.Ingredients)
        {
            Console.WriteLine($"**{ig.groupName}**");
            foreach (Recipe.Ingredient ingredient in ig.ingredients)
                Console.WriteLine($"{ingredient.quantity} {ingredient.unit} {ingredient.itemName}");
        }

        Console.WriteLine("******************************");

        return "";
    }
}