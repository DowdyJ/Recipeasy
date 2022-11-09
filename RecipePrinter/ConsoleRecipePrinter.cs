

namespace Recipeasy.RecipePrinter;

public class ConsoleRecipePrinter : IRecipePrinter
{
    public string PrintRecipe(Recipe recipe)
    {
        Console.WriteLine("******************************");

        Console.WriteLine(recipe.Title);

        Console.WriteLine("------------------------------");
        Console.WriteLine("**NUTRITION FACTS**");

        PrintNutritionFacts(recipe);

        Console.WriteLine("------------------------------");
        Console.WriteLine("**INGREDIENTS**");

        foreach (Recipe.IngredientGroup ig in recipe.Ingredients)
        {
            Console.WriteLine($"**{ig.groupName}**");
            foreach (Recipe.Ingredient ingredient in ig.ingredients)
                Console.WriteLine($"{ingredient.quantity} {ingredient.unit} {ingredient.itemName} {ingredient.extraNotes}");
        }

        Console.WriteLine("------------------------------");
        Console.WriteLine("**INSTRUCTIONS**");

        foreach (Recipe.Instruction i in recipe.Instructions)
            PrintInstructions(i);

        Console.WriteLine("******************************");

        return "";
    }

    void PrintInstructions(Recipe.Instruction instruction) 
    {
        Console.WriteLine($"{instruction.instructionSymbol} {instruction.instructionText}");
        if (instruction.subInstructions != null)
            foreach (Recipe.Instruction si in instruction.subInstructions)
                PrintInstructions(si);
    }

    void PrintNutritionFacts(Recipe recipe) 
    {
        foreach (Recipe.NutritionFact nf in recipe.NutritionFacts)
        {
            Console.WriteLine(nf.label.ToUpper() + nf.value + nf.unit);
        }
    }
}