using PuppeteerSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Recipeasy.RecipeScraper;

public class WprmRecipeScraper : IRecipeScraper
{
    public async Task<Recipe> ScrapeRecipeAsyc(IPage page, string url, RecipeScraperOptions scraperOptions)
    {
        List<String> errors = new List<string>();

        bool scrapeEverything = (scraperOptions & RecipeScraperOptions.AllContents) != RecipeScraperOptions.None;
        bool scrapeIngredients = scrapeEverything || ((scraperOptions & RecipeScraperOptions.Ingredients) != RecipeScraperOptions.None);
        bool scrapeInstruction = scrapeEverything || ((scraperOptions & RecipeScraperOptions.Instructions) != RecipeScraperOptions.None);
        bool scrapeBlog = scrapeEverything || ((scraperOptions & RecipeScraperOptions.Blog) != RecipeScraperOptions.None);
        bool scrapeComments = scrapeEverything || ((scraperOptions & RecipeScraperOptions.Comments) != RecipeScraperOptions.None);
        bool scrapeNutritionFacts = scrapeEverything || ((scraperOptions & RecipeScraperOptions.NutritionFacts) != RecipeScraperOptions.None);
        bool scrapeMetaData = scrapeEverything || ((scraperOptions & RecipeScraperOptions.MetaData) != RecipeScraperOptions.None);

        bool useMetric = (scraperOptions & RecipeScraperOptions.Metric) != RecipeScraperOptions.None;
        bool usePictures = (scraperOptions & RecipeScraperOptions.IncludePictures) != RecipeScraperOptions.None;


        Recipe recipe = new Recipe();

        Task<List<String>> instructionsTask;
        if (scrapeInstruction) 
            instructionsTask = Task.Run<List<String>>(() => { return new List<String>(); });
        else
            instructionsTask = Task.Run<List<String>>(() => { return new List<String>(); });


        Task<List<Recipe.IngredientGroup>> ingredientsTask;
        if (scrapeIngredients)
            ingredientsTask = ExtractIngredientGroupsFromPage(page, useMetric, errors); 
        else
            ingredientsTask = Task.Run<List<Recipe.IngredientGroup>>(() => { return new List<Recipe.IngredientGroup>(); });


        await Task.WhenAll(instructionsTask, ingredientsTask);

        recipe.Instructions = instructionsTask.Result;
        recipe.Ingredients = ingredientsTask.Result;



        return recipe;
    }

    private async Task<List<String>> ExtractInstructionsFromPage(IPage page, List<String> errors) 
    {

        return null;
    }


    private async Task<List<Recipe.IngredientGroup>> ExtractIngredientGroupsFromPage(IPage page, bool metric, List<String> errors) 
    {
        if (metric)
        {
            IElementHandle metricButton = await page.QuerySelectorAsync("button[aria-label='Change unit system to Metric']");
            if (metricButton is null)
            {
                errors.Add("Couldn't find metric conversion button.");
            }
            else 
            {
                await metricButton.ClickAsync();
                await Task.Delay(200);
            }
        }
        else //standard
        {
            IElementHandle standardUnitsButton = await page.QuerySelectorAsync("button[aria-label='Change unit system to US Customary']");
            if (standardUnitsButton is null)
            {
                errors.Add("Couldn't find standard units conversion button.");
            }
            else 
            {
                await standardUnitsButton.ClickAsync();
                await Task.Delay(200);
            }
        }

        IElementHandle[] recipeGroups = await page.QuerySelectorAllAsync("div[class='wprm-recipe-ingredient-group']");
        
        List<Recipe.IngredientGroup> ingredientGroups = new List<Recipe.IngredientGroup>();

        foreach (IElementHandle element in recipeGroups)
        {
            string? titleText = (string?)(await element.EvaluateFunctionAsync("e => {try {var a = e.querySelectorAll(\"h4\"); return a[0].innerText} catch(err) {}}"));
            
            if (titleText is null)
            {
                errors.Add($"Failed to extract title on {element.ToString()}.");
                titleText = "TITLE";
            }

            string? ingredientJson = (string?) await element.EvaluateFunctionAsync(
                "e => {try{function extractQuantity(liNode){try{var amount = liNode.querySelector(\"span[class='wprm-recipe-ingredient-amount']\").innerText;return amount;}catch (err){return \"\";}}function extractUnit(liNode){try{var unit = liNode.querySelector(\"span[class='wprm-recipe-ingredient-unit']\").innerText;return unit;}catch (err){return \"\";}}function extractExtraNotes(liNode){try{var extraNotes = liNode.querySelector(\"span[class*='wprm-recipe-ingredient-notes']\").innerText;return extraNotes;}catch(err){return\"\";}}function extractName(liNode){try{var name = liNode.querySelector(\"span[class='wprm-recipe-ingredient-name']\").innerText;return name;}catch (err){return \"\";}}var ingredientNodes = e.querySelectorAll(\"li[class='wprm-recipe-ingredient']\");class Ingredient{constructor(quantity, unit, name, extraNotes){this.quantity = quantity;this.unit = unit;this.name = name;this.extraNotes = extraNotes;}}const ingredientsList = [];for (let i = 0; i < ingredientNodes.length; i++){var quantity = extractQuantity(ingredientNodes[i]);var unit = extractUnit(ingredientNodes[i]);var name = extractName(ingredientNodes[i]);var extraNotes = extractExtraNotes(ingredientNodes[i]);ingredientsList[i] = new Ingredient(quantity, unit, name, extraNotes);}var jsonStringOfIngredientsList = JSON.stringify(ingredientsList);return jsonStringOfIngredientsList;}catch(err){}}");
 
            if (ingredientJson is null)
            {
                errors.Add($"Failed to extract ingredients on {element.ToString()}");
                ingredientJson = "{IngredientList:[]}";
            } else 
            {
                ingredientJson = ingredientJson.Insert(0, "{IngredientList:");
                ingredientJson = ingredientJson.Insert(ingredientJson.Length,"}");
            }

            JObject parsedIngredients = JObject.Parse(ingredientJson);

            Recipe.IngredientGroup newGroup = new Recipe.IngredientGroup();
            newGroup.groupName = titleText;
            newGroup.ingredients = new List<Recipe.Ingredient>();

            foreach (var ingredient in parsedIngredients["IngredientList"]) 
            {
                newGroup.ingredients.Add(new Recipe.Ingredient(((string?)ingredient["quantity"]), ((string?)ingredient["unit"]), ((string?)ingredient["name"]), ((string?)(ingredient["extraNotes"]))));
            }

            ingredientGroups.Add(newGroup);
        }
        return ingredientGroups;
    }
}