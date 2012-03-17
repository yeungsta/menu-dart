using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using MenuDart.Composer;

namespace MenuDart.Models
{
    public class MDInitializer : DropCreateDatabaseIfModelChanges<MenuDartDBContext>
    {
        protected override void Seed(MenuDartDBContext context)
        {
            //create a location
            Location location1 = new Location();
            location1.Title = "The Buffalo Fire Department<br>is located across from The Depot Restaurant at:";
            location1.Address = "1261 Cabrillo Avenue";
            location1.City = "Torrance";
            location1.State = "CA";
            location1.Zip = "90501";
            location1.MapImgUrl = "http://maps.googleapis.com/maps/api/staticmap?size=275x275&maptype=roadmap\\&markers=size:mid%7Ccolor:red%7C1261%20Cabrillo%20Avenue,Torrance,CA&sensor=false&zoom=14";
            location1.MapLink = "http://maps.google.com/maps?q=1261%20Cabrillo%20Avenue,Torrance,CA";
            location1.Hours = "Monday - Friday for lunch:<br>11am - 2pm<p>Monday - Thursday for dinner:<br>5pm - 9pm<p>Friday - Saturday for dinner:<br>5pm - 10pm<p>The Bar Happy Hour<br>Monday - Friday:<br>5pm - 7pm";
            location1.Phone = "310-320-2332";

            //create a location
            Location location2 = new Location();
            location2.Title = "2nd Location";
            location2.Address = "4321 Cabrillo Avenue";
            location2.City = "Gardena";
            location2.State = "CA";
            location2.Zip = "76006";
            location2.MapImgUrl = "http://maps.googleapis.com/maps/api/staticmap?size=275x275&maptype=roadmap\\&markers=size:mid%7Ccolor:red%7C1261%20Cabrillo%20Avenue,Torrance,CA&sensor=false&zoom=14";
            location2.MapLink = "http://maps.google.com/maps?q=1261%20Cabrillo%20Avenue,Torrance,CA";
            location2.Hours = "Monday - Friday for lunch:<br>11am - 2pm<p>Monday - Thursday for dinner:<br>5pm - 9pm<p>Friday - Saturday for dinner:<br>5pm - 10pm<p>The Bar Happy Hour<br>Monday - Friday:<br>5pm - 7pm";
            location2.Phone = "310-320-2332";

            //add to locations
            List<Location> locations = new List<Location>();
            locations.Add(location1);
            locations.Add(location2);

            //build leaf lists
            List<MenuNode> firestarters = new List<MenuNode>();
            firestarters.Add(new MenuLeaf() { Title = "Chef's Crispy Mac & Cheese Balls", Description = "With BFD Ranch Dip", Price = 7, Link = "1-1-i1" });
            firestarters.Add(new MenuLeaf() { Title = "Field Greens", Description = "With Ginger Corn Dressing", Price = 6, Link = "1-1-i2" });
            firestarters.Add(new MenuLeaf() { Title = "Caesar Salad", Price = 6, Link = "1-1-i3" });
            firestarters.Add(new MenuLeaf() { Title = "Chopped Salad", Description = "With Blue Cheese Dressing", Price = 6, Link = "1-1-i4" });
            firestarters.Add(new MenuLeaf() { Title = "Bowl of Chili", Description = "With Cheese & Onions (Add $1)", Price = 5, Link = "1-1-i5" });
            firestarters.Add(new MenuLeaf() { Title = "BFD Fried Chicken Chunks", Description = "With Blue Cheese Dip", Price = 7, Link = "1-1-i6" });
            firestarters.Add(new MenuLeaf() { Title = "Fries", Price = 4, Link = "1-1-i7" });
            firestarters.Add(new MenuLeaf() { Title = "Chili Fries", Price = 6, Link = "1-1-i8" });
            firestarters.Add(new MenuLeaf() { Title = "BFD Fries", Price = 6, Link = "1-1-i9" });
            firestarters.Add(new MenuLeaf() { Title = "Sloppy Fries", Description = "With Chili, Cheese, Onions, Guacamole, Salsa, Secret Sauce", Price = 8, Link = "1-1-i10" });
            firestarters.Add(new MenuLeaf() { Title = "Crispy Fried Shaved Onions", Price = 5, Link = "1-1-i11" });

            List<MenuNode> wings = new List<MenuNode>();
            wings.Add(new MenuLeaf() { Title = "Wings By the Dozen", Description = "Order 'Em The Way You Like 'Em<br>Mild / Medium / Hot<br>With Bleu Cheese Dip and Celery Sticks", Price = 10, Link = "1-2-i1" });

            List<MenuNode> salads = new List<MenuNode>();
            salads.Add(new MenuLeaf() { Title = "Southwest Chicken Salad", Description = "With Citrus Vinaigrette", Price = 12, Link = "1-3-i1" });
            salads.Add(new MenuLeaf() { Title = "Buffalo Chicken Chunk Wedge Salad", Price = 12, Link = "1-3-i2" });
            salads.Add(new MenuLeaf() { Title = "Crispy Thai Chicken Salad", Description = "With Tomatoes, Onions, Cucumber, Greens and Ginger Corn Dressing", Price = 14, Link = "1-3-i3" });
            salads.Add(new MenuLeaf() { Title = "Caesar", Description = "Regular", Price = 11, Link = "1-3-i4" });
            salads.Add(new MenuLeaf() { Title = "Caesar", Description = "With Southwest Chicken", Price = 14, Link = "1-3-i5" });
            salads.Add(new MenuLeaf() { Title = "Caesar", Description = "With Buffalo Chicken", Price = 14, Link = "1-3-i6" });
            salads.Add(new MenuLeaf() { Title = "Caesar", Description = "With Buffalo Chicken Chunks", Price = 14, Link = "1-3-i7" });
            salads.Add(new MenuLeaf() { Title = "Caesar", Description = "With Seared Ahi", Price = 16, Link = "1-3-i8" });
            salads.Add(new MenuLeaf() { Title = "Caesar", Description = "With Burger", Price = 14, Link = "1-3-i9" });
            salads.Add(new MenuLeaf() { Title = "Caesar", Description = "With Grilled Portobello", Price = 14, Link = "1-3-i10" });
            salads.Add(new MenuLeaf() { Title = "Buffalo Chicken and Blue Cheese Chopped Salad", Price = 14, Link = "1-3-i11" });
            salads.Add(new MenuLeaf() { Title = "Seared Ahi Salad", Description = "With Ginger Corn Dressing and Avocado", Price = 16, Link = "1-3-i12" });

            List<MenuNode> burgers = new List<MenuNode>();
            burgers.Add(new MenuLeaf() { Title = "The Burger", Description = "With Secret Sauce", Price = 8, Link = "1-4-i1" });
            burgers.Add(new MenuLeaf() { Title = "The Cheeseburger", Description = "With Secret Sauce", Price = 9, Link = "1-4-i2" });
            burgers.Add(new MenuLeaf() { Title = "Double Cheeseburger", Description = "With Secret Sauce", Price = 13, Link = "1-4-i3" });
            burgers.Add(new MenuLeaf() { Title = "Chili Cheeseburger", Price = 11, Link = "1-4-i4" });
            burgers.Add(new MenuLeaf() { Title = "Sloppy Burger", Description = "Chili, Cheese, Onions, Guacamole, Salsa, Secret Sauce", Price = 13, Link = "1-4-i5" });
            burgers.Add(new MenuLeaf() { Title = "Blue Cheese and Portobello Burger", Description = "With Secret Sauce", Price = 13, Link = "1-4-i6" });
            burgers.Add(new MenuLeaf() { Title = "Swiss, Mushroom and Onion Burger", Description = "With Secret Sauce", Price = 11, Link = "1-4-i7" });
            burgers.Add(new MenuLeaf() { Title = "BBQ Bacon and Crispy Onion Cheeseburger", Price = 12, Link = "1-4-i8" });
            burgers.Add(new MenuLeaf() { Title = "Thai BBQ and Wasabi Sauce Burger", Price = 11, Link = "1-4-i9" });
            burgers.Add(new MenuLeaf() { Title = "Guacamole Grilled Zucchini Burger", Description = "With Secret Sauce", Price = 11, Link = "1-4-i10" });
            burgers.Add(new MenuLeaf() { Title = "Guacamole Jalapeno Cheeseburger", Description = "With Secret Sauce", Price = 11, Link = "1-4-i11" });
            burgers.Add(new MenuLeaf() { Title = "Roasted Pepper and Onion Mozzarella Burger", Description = "With Secret Sauce", Price = 11, Link = "1-4-i12" });
            burgers.Add(new MenuLeaf() { Title = "Mac & Cheese and Burger Stack", Description = "With Ranch", Price = 11, Link = "1-4-i13" });
            burgers.Add(new MenuLeaf() { Title = "Grilled Tomatoes, Mozzarella and Roasted Garlic Clove Burger", Description = "With Secret Sauce", Price = 11, Link = "1-4-i14" });
            burgers.Add(new MenuLeaf() { Title = "Burger Rancheros", Description = "With Fried Egg, Salsa, Guacamole and Cheese", Price = 12, Link = "1-4-i15" });
            burgers.Add(new MenuLeaf() { Title = "Chopped Burger Bowl", Description = "With Fries, Chili, Cheese, L.T.O. and a Fried Egg", Price = 13, Link = "1-4-i16" });
            burgers.Add(new MenuLeaf() { Title = "BFD Burger and Buffalo Chicken", Description = "With Bleu Cheese and Fried Onions", Price = 13, Link = "1-4-i17" });
            burgers.Add(new MenuLeaf() { Title = "Slim's Famous Speed Burger", Description = "Triple Cheese, Triple Bacon and Secret Sauce", Price = 13, Link = "1-4-i18" });
            burgers.Add(new MenuLeaf() { Title = "Holly's Burger", Description = "No Meat, Extra Cheese, Friend Onions, Jalapenos and Secret Sauce", Price = 7, Link = "1-4-i19" });
            burgers.Add(new MenuLeaf() { Title = "Chef's Burger", Description = "Foie Gras, Caramelized Onions, Secret Sauce and a Fried Egg", Price = 17, Link = "1-4-i20" });
            burgers.Add(new MenuLeaf() { Title = "Noah's Cheeseburger", Description = "On a Bun, Plain", Price = 7, Link = "1-4-i21" });
            burgers.Add(new MenuLeaf() { Title = "Max's Cheeseburger", Description = "Caramelized Onions, Fried Onions, Extra Sauce and Extra Cheese", Price = 12, Link = "1-4-i22" });
            burgers.Add(new MenuLeaf() { Title = "Spencer's Burger", Description = "Double Cheese with Extra Secret Sauce and Nothing Else", Price = 10, Link = "1-4-i23" });
            burgers.Add(new MenuLeaf() { Title = "Nancy's Burger", Description = "Any Burger, As Long As She Doesn't Have to Clean It Up", Link = "1-4-i24" });

            List<MenuNode> specialties = new List<MenuNode>();
            specialties.Add(new MenuLeaf() { Title = "Garlic Shrimp Burger", Description = "With Wasabi Scallion Sauce", Price = 16, Link = "1-5-i1" });
            specialties.Add(new MenuLeaf() { Title = "Chicken Burger", Description = "With Guacamole, L.T.O. and Secret Sauce", Price = 11, Link = "1-5-i2" });
            specialties.Add(new MenuLeaf() { Title = "Southwest Chicken Sandwich", Description = "With Guacamole, Salsa and Cheese", Price = 12, Link = "1-5-i3" });
            specialties.Add(new MenuLeaf() { Title = "Seared Ahi Steak Sandwich", Description = "With Depot Chee and Ginger Sauce", Price = 16, Link = "1-5-i4" });
            specialties.Add(new MenuLeaf() { Title = "PLT", Description = "Portobello, Lettuce, Tomato, Bleu Cheese", Price = 8, Link = "1-5-i5" });
            specialties.Add(new MenuLeaf() { Title = "Macaroni and Cheese Fritter", Description = "With L.T.O. and Ranch", Price = 8, Link = "1-5-i6" });
            specialties.Add(new MenuLeaf() { Title = "Buffalo Chicken Sandwich", Description = "With L.T.O. and Bleu Cheese Dressing", Price = 12, Link = "1-5-i7" });
            specialties.Add(new MenuLeaf() { Title = "Grilled Polish Sausage", Description = "With Roasted Peppers and Onions", Price = 9, Link = "1-5-i8" });
            specialties.Add(new MenuLeaf() { Title = "Chili Dog", Description = "With Cheese and Onions", Price = 7, Link = "1-5-i9" });
            specialties.Add(new MenuLeaf() { Title = "Alpine Village Hot Dog", Description = "U Top It", Price = 6, Link = "1-5-i10" });

            List<MenuNode> beverages = new List<MenuNode>();
            beverages.Add(new MenuLeaf() { Title = "Ice Cream Floats", Description = "Coke or Diet, Root Beer, Orange Soda, Chocolate", Price = 5, Link = "1-6-i1" });
            beverages.Add(new MenuLeaf() { Title = "Soda", Description = "Coke or Diet, Root Beer, Sprite, Ginger Ale, Orange", Price = 2, Link = "1-6-i2" });
            beverages.Add(new MenuLeaf() { Title = "Mineral Water", Price = 3, Link = "1-6-i3" });
            beverages.Add(new MenuLeaf() { Title = "Iced Tea", Price = 2, Link = "1-6-i4" });
            beverages.Add(new MenuLeaf() { Title = "Coffee", Price = 2, Link = "1-6-i5" });
            beverages.Add(new MenuLeaf() { Title = "Tea", Price = 2, Link = "1-6-i6" });
            beverages.Add(new MenuLeaf() { Title = "Expresso", Price = 5, Link = "1-6-i7" });
            beverages.Add(new MenuLeaf() { Title = "Latte", Price = 5, Link = "1-6-i8" });
            beverages.Add(new MenuLeaf() { Title = "Cappuccino", Price = 5, Link = "1-6-i9" });

            List<MenuNode> sweets = new List<MenuNode>();
            sweets.Add(new MenuLeaf() { Title = "Triple Chocolate Peanut Butter Cake", Price = 7, Link = "1-7-i1" });
            sweets.Add(new MenuLeaf() { Title = "Hot Apple Cranberry Cobbler", Description = "With Ice Cream", Price = 7, Link = "1-7-i2" });
            sweets.Add(new MenuLeaf() { Title = "Campfire Smores", Price = 7, Link = "1-7-i3" });
            sweets.Add(new MenuLeaf() { Title = "Chocolate Chip Cookie Ice Cream Sandwich", Description = "With Fudge Dip", Price = 7, Link = "1-7-i4" });

            //create menutree
            List<MenuNode> menutree = new List<MenuNode>();
            menutree.Add(new MenuNode() { Title = "Fire Starters", Link = "1-1", Branches = firestarters });
            menutree.Add(new MenuNode() { Title = "Buffalo Fire Wing Dept.", Link = "1-2", Branches = wings });
            menutree.Add(new MenuNode() { Title = "Big Salads", Link = "1-3", Branches = salads });
            menutree.Add(new MenuNode() { Title = "Burgers", Link = "1-4", Text = "All Our Burgers Are 8 Oz. Angus Beef Ground Chuck, Served On Freshly Baked Firehouse Buns with L.T.O. and Fries<p>Substitute Chicken Burger or Portobello for Beef on any burger.", Branches = burgers });
            menutree.Add(new MenuNode() { Title = "BFD Specialties", Link = "1-5", Text = "Served with Fries and L.T.O.", Branches = specialties });
            menutree.Add(new MenuNode() { Title = "Beverages", Link = "1-6", Branches = beverages });
            menutree.Add(new MenuNode() { Title = "Sweets", Link = "1-7", Branches = sweets });

            var menus = new List<Menu> {  
                 new Menu { 
                     Name = "Buffalo Fire Department",
                     City = "Torrance",
                     Phone = "310-320-2332",
                     Website = "http://www.buffalofiredepartment.com",
                     AboutTitle = "Welcome to\r\nThe Buffalo Fire Department!",
                     AboutText = "This is the story of the The Buffalo Fire Department, Chef Shafer's second restaurant.\r\n\r\nThe chef has always wanted to own an adult-style burger joint where you can get a great burger with a serious wine list and a full bar in an atmosphere that isn't tropically or corporately enhanced. He eloquently states, \"This is the kind of place you can conduct business in and still use as a great watering hole\".\r\n\r\nThe chef grew up in Buffalo New York, When he was young and arguing with his 4 sisters he would tell them BFD. His mom would say, \"Watch your mouth.\" and he would reply, \"I'm just saying buffalo fire department mom!!!!\"\r\n\r\nNow BFD stands for burgers fries and drinks. When creating this new joint the chef called upon the fire departments of both Buffalo and Torrance to donate items to the restaurant and they were both unbelievably supportive. They cleaned out their closests and gave the chef some great artifacts for the decor of BFD.\r\n\r\nNow the dream comes alive in the newest joint in town!",
                     Email = "chef@buffalofiredepartment.com",
                     Facebook = "http://www.facebook.com/people/Buffalo-FireDepart/100000963304030",
                     Twitter = "https://twitter.com/buffalofire",
                     Yelp = "http://www.yelp.com/biz/buffalo-fire-department-torrance",
                     Template = "Jalapeno",
                     Owner = "chef@buffalofiredepartment.com",
                     MenuDartUrl = "buffalo-fire-department-torrance",
                     Locations = V1.SerializeLocations(locations),
                     MenuTree = V1.SerializeMenuTree(menutree)
                 },

                 new Menu {
                     Name = "Depot",
                     City = "Torrance",
                     Phone = "310-787-7501",
                     Website = "http://www.depotrestaurant.com",
                     AboutTitle = "Live Well, Love Much, Laugh Often",
                     AboutText = "Depot is not only the name of the restaurant where Chef Michael Shafer is the iron horse and engineer, it's the creative hub of culinary lines from all over the world that have marked his career, from Classical cuisine to German, Austrian, contemporary Scandinavian, pan-Asian, and Mexican to American cooking from the East Coast to the Southwest and California.\r\n\r\nAt Depot we smoke our own meats, make our own sausages, and our bread rises to your table. And don't forget to save room for our fresh pastries, ice creams and desserts.\r\n\r\nDepot is available for private parties, meetings, and toga parties. Ask for our Party Brochure or phone for a consultation.\r\n\r\nLearn to cook, the Shafer way... at the Chef's fun monthly cooking classes. Sign up today, they always sell out!",
                     Email = "info@depotrestaurant.com",
                     Facebook = "http://www.facebook.com/pages/Depot-Restaurant/113548155346035",
                     Twitter = "",
                     Yelp = "http://www.yelp.com/biz/depot-restaurant-torrance",
                     //Hours = "*Email reservations are not accepted\r\n\r\nDepot is open for lunch and dinner:\r\n\r\nMonday - Friday, 11am - 2pm\r\n\r\nMonday - Thursday, 5:30pm - 9pm\r\n\r\nFriday - Saturday, 5:30pm - 10pm\r\n\r\nClosed Sundays",
                     Template = "Eggplant",
                     Owner = "info@depotrestaurant.com",
                     MenuDartUrl = "depot-torrance",
                     Locations = "XML here",
                     MenuTree = "XML here"
                 },
             };

            menus.ForEach(d => context.Menus.Add(d));

            var templates = new List<Template> {  
                 new Template { 
                     Name = "Jalapeno",
                     BkgndClr = "#C80000",
                     HdrTxtClr = "white",
                     HdrTxtFnt = "sans-serif",
                     HdrClrGrdntTop = "red",
                     HdrClrGrdntBottom = "maroon",
                     FtrTxtClr = "white",
                     FtrLnkClr = "",
                     Stripes = "vertical"
                 },
                 new Template { 
                     Name = "Eggplant",
                     BkgndClr = "#6B246B",
                     HdrTxtClr = "white",
                     HdrTxtFnt = "sans-serif",
                     HdrClrGrdntTop = "",
                     HdrClrGrdntBottom = "",
                     FtrTxtClr = "white",
                     FtrLnkClr = "",
                     Stripes = "vertical"
                 },
                 new Template { 
                     Name = "Ice",
                     BkgndClr = "#3385D6",
                     HdrTxtClr = "white",
                     HdrTxtFnt = "sans-serif",
                     HdrClrGrdntTop = "",
                     HdrClrGrdntBottom = "",
                     FtrTxtClr = "white",
                     FtrLnkClr = "",
                     Stripes = "vertical"
                 },
                 new Template { 
                     Name = "Brie",
                     BkgndClr = "#DFCB97",
                     HdrTxtClr = "black",
                     HdrTxtFnt = "serif",
                     HdrClrGrdntTop = "#FBE6AD",
                     HdrClrGrdntBottom = "#AC9E77",
                     FtrTxtClr = "black",
                     FtrLnkClr = "",
                     Stripes = "vertical"
                 },
                 new Template { 
                     Name = "Marinara",
                     BkgndClr = "",
                     HdrTxtClr = "",
                     HdrTxtFnt = "",
                     HdrClrGrdntTop = "",
                     HdrClrGrdntBottom = "",
                     FtrTxtClr = "",
                     FtrLnkClr = "",
                     Stripes = ""
                 },
                 new Template { 
                     Name = "Spinach",
                     BkgndClr = "",
                     HdrTxtClr = "",
                     HdrTxtFnt = "",
                     HdrClrGrdntTop = "",
                     HdrClrGrdntBottom = "",
                     FtrTxtClr = "",
                     FtrLnkClr = "",
                     Stripes = ""
                 },
             };

            templates.ForEach(d => context.Templates.Add(d));
        }
    }
}