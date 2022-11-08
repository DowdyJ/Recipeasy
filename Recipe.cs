namespace Recipeasy;

public struct Recipe 
{
    public Recipe(String title, List<IngredientGroup> ingredients, List<String> instructions, List<Comment> comments) 
    {
        this.title = title;
        this.ingredients = ingredients;
        this.instructions = instructions;
        this.comments = comments;
    }

    public String Title { get => title; set => title = value;}
    public List<IngredientGroup> Ingredients { get => ingredients; set => ingredients = value;}
    public List<String> Instructions { get => instructions; set => instructions = value;}
    public List<Comment> Comments { get => comments; set => comments = value;}

    private String title;
    private List<IngredientGroup> ingredients;
    private List<String> instructions;
    private List<Comment> comments;

    public struct Comment 
    {
        public String commentText;
    }

    public struct IngredientGroup 
    {
        public String groupName;
        public List<Ingredient> ingredients;
    }

    public struct Ingredient 
    {
        public Ingredient(String quantity, String unit, String itemName, String extraNotes) 
        {
            this.quantity = quantity;
            this.unit = unit;
            this.itemName = itemName;
            this.extraNotes = extraNotes;
        }

        public String quantity;
        public String unit;
        public String itemName;
        public String extraNotes;
    }

    public struct BlogElement 
    {
        
    }
}