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

        Task<List<Recipe.Instruction>> instructionsTask;
        if (scrapeInstruction) 
            instructionsTask = ExtractInstructionsFromPage(page, errors);
        else
            instructionsTask = Task.Run<List<Recipe.Instruction>>(() => { return new List<Recipe.Instruction>(); });


        Task<List<Recipe.IngredientGroup>> ingredientsTask;
        if (scrapeIngredients)
            ingredientsTask = ExtractIngredientGroupsFromPage(page, useMetric, errors); 
        else
            ingredientsTask = Task.Run<List<Recipe.IngredientGroup>>(() => { return new List<Recipe.IngredientGroup>(); });

        Task<List<Recipe.NutritionFact>> nutritionFactsTask;
        if (scrapeNutritionFacts)
            nutritionFactsTask = ExtractNutritionFactsFromPage(page, errors); 
        else
            nutritionFactsTask = Task.Run<List<Recipe.NutritionFact>>(() => { return new List<Recipe.NutritionFact>(); });


        await Task.WhenAll(instructionsTask, ingredientsTask, nutritionFactsTask);

        recipe.Instructions = instructionsTask.Result;
        recipe.Ingredients = ingredientsTask.Result;
        recipe.NutritionFacts = nutritionFactsTask.Result;


        return recipe;
    }


    private async Task<List<Recipe.NutritionFact>> ExtractNutritionFactsFromPage(IPage page, List<String> errors) 
    {
        IElementHandle nutritionFactsContainer = await page.QuerySelectorAsync("div[class*='wprm-nutrition-label-container']");

        IElementHandle[] nutritionFactSections = await nutritionFactsContainer.QuerySelectorAllAsync("span[class*='wprm-nutrition-label-text-nutrition-container']");
        
        List<Recipe.NutritionFact> nutritionFacts = new List<Recipe.NutritionFact>();

        foreach (IElementHandle nutritionFact in nutritionFactSections)
        {
            String label = await nutritionFact.QuerySelectorAsync("span[class*='wprm-nutrition-label-text-nutrition-label']")?.EvaluateFunctionAsync<String>("e => e.innerText") ?? "";
            String value = await nutritionFact.QuerySelectorAsync("span[class*='wprm-nutrition-label-text-nutrition-value']")?.EvaluateFunctionAsync<String>("e => e.innerText") ?? "";
            String unit = await nutritionFact.QuerySelectorAsync("span[class*='wprm-nutrition-label-text-nutrition-unit']")?.EvaluateFunctionAsync<String>("e => e.innerText") ?? "";

            nutritionFacts.Add(new Recipe.NutritionFact(label, unit, value));
        }
        
        return nutritionFacts;
    }

    private async Task<List<Recipe.Instruction>> ExtractInstructionsFromPage(IPage page, List<String> errors) 
    {
        IElementHandle instructionsContainer = await page.QuerySelectorAsync("div[class*='wprm-recipe-instructions-container']");

        IElementHandle[] instructionGroups = await instructionsContainer.QuerySelectorAllAsync("div[class='wprm-recipe-instruction-group']");
        
        List<Recipe.Instruction> instructions = new List<Recipe.Instruction>();

        foreach (IElementHandle iGroup in instructionGroups)
        {
            String groupTitle = (string?) await iGroup.EvaluateFunctionAsync("e => {try {return e.getElementsByTagName(\"h4\")[0].innerText} catch(err){return\"\"}}") ?? "";
            
            List<Recipe.Instruction> subInstructions = new List<Recipe.Instruction>();

            String jsonResponse = await iGroup.EvaluateFunctionAsync<String>(
                "e => {try{var listOfListElements = e.getElementsByTagName(\"li\"); function extractInstructionText(li){try {return li.querySelector(\"div[class*='wprm-recipe-instruction-text']\").innerText;} catch(err) {return \"\";}}class Instruction {constructor(instructionText){this.instructionText = instructionText;}}const listOfInstructions = [];for (let i=0; i<listOfListElements.length;i++) {listOfInstructions[i] = new Instruction(extractInstructionText(listOfListElements[i]));}return JSON.stringify(listOfInstructions);}catch(err){return \"\";}}"
            );

            jsonResponse = jsonResponse.Insert(0, "{InstructionList:");
            jsonResponse = jsonResponse.Insert(jsonResponse.Length,"}");

            JObject parsedInstructions = JObject.Parse(jsonResponse);

            int i = 0;
            foreach (var instruction in parsedInstructions["InstructionList"])
            {
                subInstructions.Add(new Recipe.Instruction((++i).ToString(), (string?)instruction["instructionText"] ?? "", null));
            }
            
            instructions.Add(new Recipe.Instruction("", groupTitle, subInstructions));
        }

        return instructions;
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