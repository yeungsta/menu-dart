using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Web.UI;
using System.Xml;
using System.Xml.Serialization;
using MenuDart.Models;
using MenuDart.Controllers;

namespace MenuDart.Composer
{
    public class V1
    {
        private Menu m_menu;
        private List<MenuNode> m_menuTree;
        private List<Location> m_locations;
        private static XmlSerializer m_menuTreeSerializer;
        private static XmlSerializer m_locationsSerializer;

        private enum MenuBarItems
        {
            Menu,
            About,
            Contact
        }

        #region static members

        /// <summary>
        /// Static Constructor
        /// </summary>
        static V1()
        {
            m_menuTreeSerializer = new XmlSerializer(typeof(List<MenuNode>));
            m_locationsSerializer = new XmlSerializer(typeof(List<Location>));      
        }

        public static string SerializeLocations(List<Location> locations)
        {
            //serialize
            string xmlString;

            using (StringWriter textWriter = new StringWriter())
            {
                m_locationsSerializer.Serialize(textWriter, locations);
                xmlString = textWriter.ToString();
            }

            return xmlString;
        }

        public static List<Location> DeserializeLocations(string locations)
        {
            //deserialize XML locations data into Locations object
            if (!string.IsNullOrEmpty(locations))
            {
                using (StringReader textReader = new StringReader(locations))
                {
                    object output = m_locationsSerializer.Deserialize(textReader);
                    return output as List<Location>;
                }
            }

            return null;
        }

        public static string SerializeMenuTree(List<MenuNode> menuTree)
        {
            //serialize
            string menuTreeString;

            using (StringWriter textWriter = new StringWriter())
            {
                m_menuTreeSerializer.Serialize(textWriter, menuTree);
                menuTreeString = textWriter.ToString();
            }

            return menuTreeString;
        }

        public static List<MenuNode> DeserializeMenuTree(string menuTree)
        {
            using (StringReader textReader = new StringReader(menuTree))
            {
                object output = m_menuTreeSerializer.Deserialize(textReader);
                return output as List<MenuNode>;
            }
        }

        public static MenuNode FindMenuNode(List<MenuNode> menuNodes, string link)
        {
            string[] levels = link.Split('-');

            List<MenuNode> currentLevel = menuNodes;
            string currentLink = string.Empty;

            //traverse to the correct level
            for (int x = 2; x < levels.Count(); x++)
            {
                //construct the link
                currentLink = levels[0];

                for (int y = 1; y < x; y++)
                {
                    currentLink += "-" + levels[y];
                }

                //get the nodes of this link
                currentLevel = currentLevel.Find(node => node.Link == currentLink).Branches;
            }

            return currentLevel.Find(node => node.Link == link);
        }

/*  Is this used?
        public static void RemoveMenuNode(List<MenuNode> menuNodes, string link)
        {
            string[] levels = link.Split('-');

            List<MenuNode> currentLevel = menuNodes;
            string currentLink = string.Empty;

            //traverse to the correct level
            for (int x = 2; x < levels.Count(); x++)
            {
                //construct the link
                currentLink = levels[0];

                for (int y = 1; y < x; y++)
                {
                    currentLink += "-" + levels[y];
                }

                //get the nodes of this link
                currentLevel = currentLevel.Find(node => node.Link == currentLink).Branches;
            }

            //remove node
            currentLevel.RemoveAll(node => node.Link == link);
        }
*/
        #endregion static members

        #region public members

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="menu">menu to compose mobile menu from</param>
        public V1(Menu menu)
        {
            m_menu = menu;
        }

        //creates a preview of the menu in a new random, temp directory
        //
        //returns unique menu directory ID
        public string CreateTempMenu(bool useSampleLogo)
        {
            //create a temporary location to create menu
            string randomId = Utilities.GetRandomId();
            string filepath = HttpContext.Current.Server.MapPath(Controllers.Constants.PreviewMenusPath + randomId + "/");

            if (!Directory.Exists(filepath))
            {
                Directory.CreateDirectory(filepath);
            }

#if !UseAmazonS3
            //copy a sample logo
            if (useSampleLogo)
            {
                CopySampleLogoFile(filepath);
            }
            else
            {
                //if existing inactive logo file exists from previous activity, copy to preview area
                string menuPath = HttpContext.Current.Server.MapPath(Controllers.Constants.MenusPath + m_menu.MenuDartUrl);

                if (LogoExists(menuPath))
                {
                    string logoPath = menuPath + "/" + Constants.ActiveLogoPath;

                    System.IO.File.Copy(logoPath, filepath + Controllers.Constants.LogoFileName, true);
                }
                else //clear any old logo file
                {
                    RemoveLogoFileFromTemp(filepath);
                }
            }
#endif

            //write menu string to file
#if UseAmazonS3
            WriteToFile(RenderMenu(m_menu.MenuDartUrl, true, useSampleLogo), filepath);
#else
            WriteToFile(RenderMenu(filepath, true, useSampleLogo), filepath);
#endif

            // return random menu directory ID
            return randomId;
        }

        //updates a preview of the menu in an existing temp directory
        public void UpdateTempMenu(string randomId, bool useSampleLogo)
        {
            //set temporary location
            string filepath = HttpContext.Current.Server.MapPath(Controllers.Constants.PreviewMenusPath + randomId + "/");
#if !UseAmazonS3
            //copy a sample logo
            if (useSampleLogo)
            {
                CopySampleLogoFile(filepath);
            }
            else
            {
                //if existing inactive logo file exists from previous activity, copy to preview area
                string menuPath = HttpContext.Current.Server.MapPath(Controllers.Constants.MenusPath + m_menu.MenuDartUrl);

                if (LogoExists(menuPath))
                {
                    string logoPath = menuPath + "/" + Constants.ActiveLogoPath;

                    System.IO.File.Copy(logoPath, filepath + Controllers.Constants.LogoFileName, true);
                }
                else //clear any old logo file
                {
                    RemoveLogoFileFromTemp(filepath);
                }
            }
#endif

            //write menu string to file
#if UseAmazonS3
            WriteToFile(RenderMenu(m_menu.MenuDartUrl, true, useSampleLogo), filepath);
#else
            WriteToFile(RenderMenu(filepath, true, useSampleLogo), filepath);
#endif
        }

/* Not used anymore?
        //create a new file location (reserving menu URL)
        //
        //returns URL to menu
        public string CreateMenuDir()
        {
            //active directories
            string filepath = HttpContext.Current.Server.MapPath(Controllers.Constants.MenusPath + m_menu.MenuDartUrl + "/");
            string urlpath = Utilities.GetFullUrl(m_menu.MenuDartUrl);

            //create directory
            if (!Directory.Exists(filepath))
            {
                Directory.CreateDirectory(filepath);
            }

            // return URL to menu
            return urlpath;
        }
*/
        //creates menu in active location
        //
        //returns URL to menu
        public string CreateMenu()
        {
#if UseAmazonS3
            string urlpath = Utilities.GetFullUrl(m_menu.MenuDartUrl);

            //write menu string to S3
            Utilities.WritePlainTextObjectToS3(RenderMenu(m_menu.MenuDartUrl, false, false), m_menu.MenuDartUrl + "/index.html");
#else
            string urlpath = Utilities.GetFullUrl(m_menu.MenuDartUrl);

            //active directories
            string filepath = HttpContext.Current.Server.MapPath(Controllers.Constants.MenusPath + m_menu.MenuDartUrl + "/");

            //write menu string to file
            WriteToFile(RenderMenu(filepath, false, false), filepath);
#endif
            // return URL to menu
            return urlpath;
        }

        #endregion public members

        #region private members

        //actual menu HTML creation/rendering
        //filepath is path to the logo file
        private string RenderMenu(string filepath, bool isPreview, bool useSampleLogo)
        {
            StringWriter stringWriter = new StringWriter();

            //just have one space as the tab
            using (HtmlTextWriter writer = new HtmlTextWriter(stringWriter, " "))
            {
                Initialize();
                CreateBase(writer);
                CreateMain(writer);
                writer.WriteLine();
                writer.WriteLine();
                CreateAbout(writer, filepath, isPreview, useSampleLogo);
                writer.WriteLine();
                writer.WriteLine();
                CreateContact(writer);
                writer.WriteLine();
                writer.WriteLine();
                AddMenuPages(writer, m_menuTree);
                writer.RenderEndTag(); // </body>
                writer.RenderEndTag(); // </html>
            }

            return stringWriter.ToString();
        }

        private void Initialize()
        {
            //deserialize XML menu data into menu tree object
            if (!string.IsNullOrEmpty(m_menu.MenuTree))
            {
                m_menuTree = DeserializeMenuTree(m_menu.MenuTree);
            }

            //deserialize XML locations data into Locations object
            if (!string.IsNullOrEmpty(m_menu.Locations))
            {
                m_locations = DeserializeLocations(m_menu.Locations);
            }
        }

        private void CreateBase(HtmlTextWriter writer)
        {
            writer.Write(Constants.DocType);
            writer.WriteLine();
            writer.RenderBeginTag(HtmlTextWriterTag.Html);
            writer.RenderBeginTag(HtmlTextWriterTag.Head);
                    AddTitle(writer, m_menu.Name);
                    AddMetaEncoding(writer);
                    AddMeta(writer, "viewport", "width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=0");
                    AddMeta(writer, "apple-mobile-web-app-capable", "yes");
                    AddMeta(writer, "apple-mobile-web-app-status-bar-style", "black");
                    AddLink(writer, "stylesheet", Constants.JqueryMobileCss, null);
                    AddLink(writer, "stylesheet", Constants.MasterCssPath, "text/css");

                    //if a template is defined, add template stylesheet
                    if (!string.IsNullOrEmpty(m_menu.Template))
                    {
                        AddLink(writer, "stylesheet", Constants.TemplateCssPath + Constants.TemplateCssName + m_menu.Template.ToLower() + Constants.CssExtension, "text/css");
                    }
                    else
                    {
                        AddLink(writer, "stylesheet", Constants.TemplateCssPath + Constants.TemplateCssName + Constants.DefaultTemplate + Constants.CssExtension, "text/css");
                    }

                    AddScript(writer, Constants.Jquery);
                    AddScript(writer, Constants.JqueryMobile);
            writer.RenderEndTag();
            writer.WriteLine();
            writer.RenderBeginTag(HtmlTextWriterTag.Body);
        }

        private void CreateMain(HtmlTextWriter writer)
        {
            BeginPage(writer, MenuBarItems.Menu.ToString(), "bodyBg");
                BeginHeader(writer);
                    AddHeaderBar(writer, "headerBar", m_menu.Name);
                    writer.WriteLine();
                    AddMenuBar(writer, MenuBarItems.Menu);
                writer.RenderEndTag();
                writer.WriteLine();
                BeginContent(writer);
                    AddMenuList(writer, m_menuTree);
                writer.RenderEndTag();
                    writer.WriteLine();
                    AddCallBtn(writer, m_menu.Phone);
                    writer.WriteLine();
                    AddOrigSiteBtn(writer);
                    writer.WriteLine();
                    writer.WriteBreak();
                    writer.WriteBreak();
                    writer.WriteBreak();
                    writer.WriteLine();
                    AddFooter(writer);
            writer.RenderEndTag();
        }

        private void CreateAbout(HtmlTextWriter writer, string filepath, bool isPreview, bool useSampleLogo)
        {
            BeginPage(writer, MenuBarItems.About.ToString(), "bodyBg");
            BeginHeader(writer);
                AddHeaderBar(writer, "headerBar", m_menu.Name);
                writer.WriteLine();
                AddMenuBar(writer, MenuBarItems.About);
            writer.RenderEndTag();
            writer.WriteLine();
            BeginContent(writer);
            AddLogoTitle(writer, filepath, isPreview, useSampleLogo);
                writer.WriteLine();
                AddStory(writer);
            writer.RenderEndTag();
            writer.RenderEndTag();
        }

        private void CreateContact(HtmlTextWriter writer)
        {
            BeginPage(writer, MenuBarItems.Contact.ToString(), "bodyBg");
            BeginHeader(writer);
            AddHeaderBar(writer, "headerBar", m_menu.Name);
            writer.WriteLine();
            AddMenuBar(writer, MenuBarItems.Contact);
            writer.RenderEndTag(); //end header div
            writer.WriteLine();
            BeginContent(writer);
            AddLocations(writer);
            writer.WriteLine();
            AddSharedButtons(writer);
            writer.RenderEndTag(); //end content div
            writer.RenderEndTag(); //end page div
        }

        private void WriteToFile(string data, string filepath)
        {
            if (!Directory.Exists(filepath))
            {
                Directory.CreateDirectory(filepath);
            }

            using (StreamWriter outfile = new StreamWriter(filepath + Constants.OutputFile))
            {                
                outfile.Write(data);
            }
        }

        //check if logo file exists in this path location
        private bool LogoExists(string filepath)
        {
            string logoPath = filepath + "/" + Constants.ActiveLogoPath;

#if UseAmazonS3
            return Utilities.IsObjectExistS3(logoPath);
#else    
            return File.Exists(logoPath);
#endif
        }
/*
        //check if logo file exists in current temp location
        private bool LogoExistsInTemp(string filepath)
        {
            string logoPath = filepath + Constants.ActiveLogoPath;

            return File.Exists(logoPath);
        }
*/
        //copies sample logo file (if there doesn't already exist one)
        private void CopySampleLogoFile(string destinationPath)
        {
            //default sample logo file
            string sampleLogoFile = HttpContext.Current.Server.MapPath(Controllers.Constants.SampleLogoPath);

            string destFile = Path.Combine(destinationPath, Controllers.Constants.LogoFileName);

            try
            {
                //do not overwrite an existing logo file
                System.IO.File.Copy(sampleLogoFile, destFile, false);
            }
            catch (Exception e)
            {
                //ignore exceptions about existing files
                Utilities.LogAppError("Exception thrown while trying to copy files to destination folder.", e);
            }
        }

        //removes any previous logo file (if there exists one)
        private void RemoveLogoFileFromTemp(string destinationPath)
        {
            //copy file (but don't overwrite logo if one exists) to menu
            string destFile = Path.Combine(destinationPath, Controllers.Constants.LogoFileName);

            try
            {
                if (System.IO.File.Exists(destFile))
                {
                    System.IO.File.Delete(destFile);
                }
            }
            catch (Exception e)
            {
                //ignore exceptions
                Utilities.LogAppError("Exception thrown while trying to remove logo file in folder.", e);
            }
        }

        private void AddTitle(HtmlTextWriter writer, string title)
        {
            writer.RenderBeginTag(HtmlTextWriterTag.Title);
            writer.Write(title);
            writer.RenderEndTag();
        }

        private void AddMetaEncoding(HtmlTextWriter writer)
        {
            writer.WriteLine();
            writer.Write("<meta http-equiv=\"Content-Type\" content=\"text/html;charset=utf-8\" >");
        }

        private void AddMeta(HtmlTextWriter writer, string name, string content)
        {
            writer.WriteLine();
            if (!string.IsNullOrEmpty(name)) { writer.AddAttribute(HtmlTextWriterAttribute.Name, name); }
            if (!string.IsNullOrEmpty(content)) { writer.AddAttribute(HtmlTextWriterAttribute.Content, content); }
            writer.RenderBeginTag(HtmlTextWriterTag.Meta);
            writer.RenderEndTag();
        }

        private void AddLink(HtmlTextWriter writer, string rel, string href, string type)
        {
            writer.WriteLine();
            if (!string.IsNullOrEmpty(rel)) { writer.AddAttribute(HtmlTextWriterAttribute.Rel, rel); }
            if (!string.IsNullOrEmpty(href)) { writer.AddAttribute(HtmlTextWriterAttribute.Href, href); }
            if (!string.IsNullOrEmpty(type)) { writer.AddAttribute(HtmlTextWriterAttribute.Type, type); }
            writer.RenderBeginTag(HtmlTextWriterTag.Link);
            writer.RenderEndTag();
        }

        private void AddScript(HtmlTextWriter writer, string src)
        {
            writer.WriteLine();
            if (!string.IsNullOrEmpty(src)) { writer.AddAttribute(HtmlTextWriterAttribute.Src, src); }
            writer.RenderBeginTag(HtmlTextWriterTag.Script);
            writer.RenderEndTag();
        }

        private void BeginPage(HtmlTextWriter writer, string id, string className)
        {
            writer.AddAttribute(Constants.DataRole, Constants.PageRole);
            if (!string.IsNullOrEmpty(id)) { writer.AddAttribute(HtmlTextWriterAttribute.Id, id); }
            if (!string.IsNullOrEmpty(className)) { writer.AddAttribute(HtmlTextWriterAttribute.Class, className); }
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
        }

        private void BeginHeader(HtmlTextWriter writer)
        {
            writer.AddAttribute(Constants.DataRole, Constants.HeaderRole);
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
        }

        private void BeginContent(HtmlTextWriter writer)
        {
            writer.AddAttribute(Constants.DataRole, Constants.ContentRole);
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
        }

        private void AddHeaderBar(HtmlTextWriter writer, string className, string title)
        {
            writer.AddAttribute(Constants.DataRole, Constants.NavbarRole);
            if (!string.IsNullOrEmpty(className)) { writer.AddAttribute(HtmlTextWriterAttribute.Class, className); }
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            AddHeaderTitle(writer, title);
            writer.RenderEndTag();
        }

        private void AddHeaderTitle(HtmlTextWriter writer, string title)
        {
            writer.RenderBeginTag(HtmlTextWriterTag.Center);
            writer.RenderBeginTag(HtmlTextWriterTag.H2);
            writer.Write(title);
            writer.RenderEndTag();
            writer.RenderEndTag();
        }

        private void AddMenuBar(HtmlTextWriter writer, MenuBarItems menuBarItemSelected)
        {
            writer.AddAttribute(Constants.DataRole, Constants.NavbarRole);
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-state-persist");
            writer.AddAttribute(Constants.DataId, "MenuBar");
            writer.AddAttribute(Constants.DataPosition, Constants.PositionFixed);
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.RenderBeginTag(HtmlTextWriterTag.Ul);
            writer.RenderBeginTag(HtmlTextWriterTag.Li);
            AddMenuBarItem(writer, MenuBarItems.Menu.ToString(),
                "#" + MenuBarItems.Menu.ToString(), Constants.MenuIcon, Constants.SlideDown, 
                IsMenuBarItemSelected(MenuBarItems.Menu.ToString(), menuBarItemSelected));
            writer.RenderEndTag();
            writer.WriteLine();
            writer.RenderBeginTag(HtmlTextWriterTag.Li);
            AddMenuBarItem(writer, MenuBarItems.About.ToString(),
                "#" + MenuBarItems.About.ToString(), Constants.AboutIcon, Constants.SlideDown, 
                IsMenuBarItemSelected(MenuBarItems.About.ToString(), menuBarItemSelected));
            writer.RenderEndTag();
            writer.WriteLine();
            writer.RenderBeginTag(HtmlTextWriterTag.Li);
            AddMenuBarItem(writer, MenuBarItems.Contact.ToString(),
                "#" + MenuBarItems.Contact.ToString(), Constants.ContactIcon, Constants.SlideDown, 
                IsMenuBarItemSelected(MenuBarItems.Contact.ToString(), menuBarItemSelected));
            writer.RenderEndTag();
            writer.RenderEndTag();
            writer.RenderEndTag();
        }

        private void AddMenuPageMenuBar(HtmlTextWriter writer)
        {
            writer.AddAttribute(Constants.DataRole, Constants.NavbarRole);
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-state-persist");
            writer.AddAttribute(Constants.DataId, "MenuBar");
            writer.AddAttribute(Constants.DataPosition, Constants.PositionFixed);
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.RenderBeginTag(HtmlTextWriterTag.Ul);
            writer.RenderBeginTag(HtmlTextWriterTag.Li);
            AddMenuBarBackItem(writer, "Back",
                "#" + MenuBarItems.Menu.ToString(), Constants.BackIcon, Constants.GoBack);
            writer.RenderEndTag();
            writer.WriteLine();
            writer.RenderBeginTag(HtmlTextWriterTag.Li);
            AddMenuBarItem(writer, MenuBarItems.About.ToString(),
                "#" + MenuBarItems.About.ToString(), Constants.AboutIcon, Constants.SlideDown, false);
            writer.RenderEndTag();
            writer.WriteLine();
            writer.RenderBeginTag(HtmlTextWriterTag.Li);
            AddMenuBarItem(writer, MenuBarItems.Contact.ToString(),
                "#" + MenuBarItems.Contact.ToString(), Constants.ContactIcon, Constants.SlideDown, false);
            writer.RenderEndTag();
            writer.RenderEndTag();
            writer.RenderEndTag();
        }

        private bool IsMenuBarItemSelected(string currentBtn, MenuBarItems menuBarItemSelected)
        {
            return (currentBtn == menuBarItemSelected.ToString());
        }

        private void AddMenuBarItem(HtmlTextWriter writer, string label, string href, string icon, string transition, bool persist)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Href, href);
            writer.AddAttribute(Constants.DataIcon, icon);
            writer.AddAttribute(Constants.DataTransition, transition);
            if (persist) { writer.AddAttribute(HtmlTextWriterAttribute.Class, "ui-state-persist ui-btn-active"); }
            writer.RenderBeginTag(HtmlTextWriterTag.A);
            writer.Write(label);
            writer.RenderEndTag();
        }

        private void AddMenuBarBackItem(HtmlTextWriter writer, string label, string href, string icon, string rel)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Href, href);
            writer.AddAttribute(Constants.DataIcon, icon);
            writer.AddAttribute(Constants.DataRel, rel);
            writer.RenderBeginTag(HtmlTextWriterTag.A);
            writer.Write(label);
            writer.RenderEndTag();
        }

        private void AddPageText(HtmlTextWriter writer, string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Center);
                writer.Write(text.Replace(Constants.NewLine, Constants.Break));
                writer.RenderEndTag();
                writer.WriteLine();
            }
        }

        private void AddMenuList(HtmlTextWriter writer, List<MenuNode> nodeList)
        {
            writer.AddAttribute(Constants.DataRole, "listview");
            writer.AddAttribute(Constants.DataInset, "true");
            writer.RenderBeginTag(HtmlTextWriterTag.Ul);

            if (nodeList != null)
            {
                foreach (MenuNode node in nodeList)
                {
                    writer.RenderBeginTag(HtmlTextWriterTag.Li);
                    writer.WriteLine();
                    writer.Indent++;

                    if (node is MenuLeaf)
                    {
                        MenuLeaf leaf = node as MenuLeaf;
                        AddMenuLeaf(writer, leaf.Title, leaf.Description, leaf.Price);
                    }
                    else
                    {
                        AddMenuNode(writer, node.Title, "#" + node.Link);
                    }

                    writer.Indent--;
                    writer.RenderEndTag();
                    writer.WriteLine();
                }
            }

            writer.RenderEndTag();
        }

        private void AddMenuNode(HtmlTextWriter writer, string label, string href)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Href, href);
            writer.RenderBeginTag(HtmlTextWriterTag.A);
            writer.Write(label);
            writer.RenderEndTag();
        }

        private void AddMenuLeaf(HtmlTextWriter writer, string title, string desc, decimal? price)
        {
            if (!string.IsNullOrEmpty(title))
            {
                writer.RenderBeginTag(HtmlTextWriterTag.H3);
                writer.Write(title);
                writer.RenderEndTag();
                writer.WriteLine();
            }

            if (!string.IsNullOrEmpty(desc))
            {
                writer.RenderBeginTag(HtmlTextWriterTag.P);
                writer.Write(desc);
                writer.RenderEndTag();
                writer.WriteLine();
            }

            AddPriceTag(writer, price);
            writer.WriteLine();
        }

        private void AddPriceTag(HtmlTextWriter writer, decimal? price)
        {
            if ((price != null) && (price > 0))
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Class, Constants.CountClass);
                writer.RenderBeginTag(HtmlTextWriterTag.Span);
                writer.Write(price);
                writer.RenderEndTag();
            }
        }

        private void AddMenuPages(HtmlTextWriter writer, List<MenuNode> nodeList)
        {
            if (nodeList != null)
            {
                //Create the first level pages
                foreach (MenuNode node in nodeList)
                {
                    //if node is a leaf, then do nothing. It's the end of that branch.
                    if (!(node is MenuLeaf))
                    {
                        AddMenuPage(writer, node);
                        writer.WriteLine();
                        writer.WriteLine();
                        //Now recursively add next level (if exists)
                        AddMenuPages(writer, node.Branches);
                    }
                }
            }
        }

        private void AddMenuPage(HtmlTextWriter writer, MenuNode node)
        {
            BeginPage(writer, node.Link, "bodyBg");
            BeginHeader(writer);
            AddHeaderBar(writer, "headerBar", node.Title);
            writer.WriteLine();
            AddMenuPageMenuBar(writer);
            writer.RenderEndTag();
            writer.WriteLine();
            BeginContent(writer);
            AddPageText(writer, node.Text);
            AddMenuList(writer, node.Branches);
            writer.RenderEndTag();
            writer.RenderEndTag();
        }

        private void AddCallBtn(HtmlTextWriter writer, string phone)
        {
            if (!string.IsNullOrEmpty(phone))
            {
                AddBtn(writer, "Click to Call", "tel:" + phone, Constants.CallIcon, "insetBtn", "b", string.Empty);
            }
        }

        private void AddOrigSiteBtn(HtmlTextWriter writer)
        {
            if (!string.IsNullOrEmpty(m_menu.Website))
            {
                AddBtn(writer, "Go to Regular Website", Utilities.AddHttpPrefix(m_menu.Website) + Constants.RegularSiteRedirect, Constants.OrigSiteIcon, "insetBtn", "a", Constants.BlankTarget);
            }
        }

        private void AddBtn(HtmlTextWriter writer, string label, string href, string icon, string className, string theme, string target)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Href, href);
            writer.AddAttribute(Constants.DataRole, Constants.ButtonRole);
            if (!string.IsNullOrEmpty(className)) { writer.AddAttribute(HtmlTextWriterAttribute.Class, className); }
            writer.AddAttribute(Constants.DataIcon, icon);
            if (!string.IsNullOrEmpty(theme)) { writer.AddAttribute(Constants.DataTheme, theme); }
            if (!string.IsNullOrEmpty(target)) { writer.AddAttribute(HtmlTextWriterAttribute.Target, target); }
            writer.RenderBeginTag(HtmlTextWriterTag.A);
            writer.Write(label);
            writer.RenderEndTag();
        }

        private void AddFooter(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "menuDartFooter");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            AddMenuDartLink(writer);
            writer.RenderEndTag();
        }

        private void AddMenuDartLink(HtmlTextWriter writer)
        {
            writer.RenderBeginTag(HtmlTextWriterTag.Center);
            writer.Write("Created with ");
            writer.AddAttribute(HtmlTextWriterAttribute.Href, Constants.MenuDartUrl);
            writer.AddAttribute(HtmlTextWriterAttribute.Target, Constants.BlankTarget);
            writer.RenderBeginTag(HtmlTextWriterTag.A);
            writer.Write("Menu Dart");
            writer.RenderEndTag();
            writer.RenderEndTag();
        }

        private void AddLogoTitle(HtmlTextWriter writer, string filepath, bool isPreview, bool useSampleLogo)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "center");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);

            if (useSampleLogo)
            {
                AddSampleLogo(writer);
            }
            else if (LogoExists(filepath))
            {
                if (isPreview)
                {
                    AddDirectLogo(writer, filepath);
                }
                else
                {
                    AddLogo(writer);
                }
            }

            if (!string.IsNullOrEmpty(m_menu.AboutTitle))
            {
                writer.RenderBeginTag(HtmlTextWriterTag.H3);
                writer.Write(m_menu.AboutTitle.Replace(Constants.NewLine, Constants.Break));
                writer.RenderEndTag();
            }

            writer.RenderEndTag();
        }

        private void AddLogo(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Src, Constants.ActiveLogoPath);
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "logo");
            writer.RenderBeginTag(HtmlTextWriterTag.Img);
            writer.RenderEndTag();
            writer.WriteLine();
        }

        //references the direct S3 source
        private void AddDirectLogo(HtmlTextWriter writer, string filepath)
        {
            string logoUrl = Utilities.GetFullUrl(filepath) + "/" + Controllers.Constants.LogoFileName;
            writer.AddAttribute(HtmlTextWriterAttribute.Src, logoUrl);
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "logo");
            writer.RenderBeginTag(HtmlTextWriterTag.Img);
            writer.RenderEndTag();
            writer.WriteLine();
        }

        //references the direct sample logo
        private void AddSampleLogo(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Src, Constants.SampleLogoPath);
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "logo");
            writer.RenderBeginTag(HtmlTextWriterTag.Img);
            writer.RenderEndTag();
            writer.WriteLine();
        }

        private void AddStory(HtmlTextWriter writer)
        {
            if (!string.IsNullOrEmpty(m_menu.AboutText))
            {
                writer.RenderBeginTag(HtmlTextWriterTag.H4);
                writer.Write(m_menu.AboutText.Replace(Constants.NewLine, Constants.Break));
                writer.RenderEndTag();
            }
        }

        private void AddLocations(HtmlTextWriter writer)
        {
            if (m_locations != null)
            {
                foreach (Location location in m_locations)
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "center");
                    writer.RenderBeginTag(HtmlTextWriterTag.Div);
                    if (!string.IsNullOrEmpty(location.Title))
                    {
                        writer.RenderBeginTag(HtmlTextWriterTag.H3);
                        writer.Write(location.Title);
                        writer.RenderEndTag();
                        writer.WriteLine();
                    }
                    writer.RenderBeginTag(HtmlTextWriterTag.H4);
                    if (!string.IsNullOrEmpty(location.Address))
                    {
                        writer.Write(location.Address + Constants.Break);
                    }
                    writer.Write(location.City);
                    if (!string.IsNullOrEmpty(location.State))
                    {
                        writer.Write(", " + location.State);
                    }
                    writer.Write(" " + location.Zip);
                    writer.Write(Constants.Paragraph);
                    writer.WriteLine();

                    //recreate Google map link (in case location has changed)
                    location.MapLink = Utilities.CreateMapLink(location.Address, location.City, location.Zip);

                    //recreate Google map image link (in case location has changed)
                    location.MapImgUrl = Utilities.CreateMapImgLink(location.Address, location.City, location.Zip);

                    if (!string.IsNullOrEmpty(location.MapImgUrl) && !string.IsNullOrEmpty(location.MapLink))
                    {
                        AddMap(writer, location.MapLink, location.MapImgUrl);
                    }
                    writer.Write(Constants.Paragraph);
                    writer.WriteLine();
                    if (!string.IsNullOrEmpty(location.Phone))
                    {
                        AddCallBtn(writer, location.Phone);
                        writer.Write(Constants.Paragraph);
                        writer.WriteLine();
                    }
                    AddHours(writer, location.Hours);
                    writer.Write(Constants.Paragraph);
                    writer.WriteLine();
                    if (!string.IsNullOrEmpty(location.Email))
                    {
                        AddEmailBtn(writer, location.Email);
                        writer.Write(Constants.Paragraph);
                        writer.WriteLine();
                    }
                    if (!string.IsNullOrEmpty(location.Facebook))
                    {
                        AddFacebookBtn(writer, location.Facebook);
                        writer.Write(Constants.Paragraph);
                        writer.WriteLine();
                    }
                    if (!string.IsNullOrEmpty(location.Twitter))
                    {
                        AddTwitterBtn(writer, location.Twitter);
                        writer.Write(Constants.Paragraph);
                        writer.WriteLine();
                    }
                    if (!string.IsNullOrEmpty(location.Yelp))
                    {
                        AddYelpBtn(writer, location.Yelp);
                    }

                    writer.RenderEndTag();
                    writer.RenderEndTag();

                    //add spacing between locations
                    writer.WriteBreak();
                }
            }
        }

        private void AddSharedButtons(HtmlTextWriter writer)
        {
            //If any of these fields are populated, that means
            //they are shared across all locations (or there is
            //only one location).
            if (!string.IsNullOrEmpty(m_menu.Email))
            {
                AddEmailBtn(writer, m_menu.Email);
                writer.Write(Constants.Paragraph);
                writer.WriteLine();
            }
            if (!string.IsNullOrEmpty(m_menu.Facebook))
            {
                AddFacebookBtn(writer, m_menu.Facebook);
                writer.Write(Constants.Paragraph);
                writer.WriteLine();
            }
            if (!string.IsNullOrEmpty(m_menu.Twitter))
            {
                AddTwitterBtn(writer, m_menu.Twitter);
                writer.Write(Constants.Paragraph);
                writer.WriteLine();
            }
            if (!string.IsNullOrEmpty(m_menu.Yelp))
            {
                AddYelpBtn(writer, m_menu.Yelp);
            }
        }

        private void AddMap(HtmlTextWriter writer, string mapLink, string imgLink)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Class, "mapImg");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
            writer.AddAttribute(HtmlTextWriterAttribute.Href, mapLink);
            writer.AddAttribute(HtmlTextWriterAttribute.Target, Constants.BlankTarget);
            writer.RenderBeginTag(HtmlTextWriterTag.A);
            writer.WriteLine();
            writer.AddAttribute(HtmlTextWriterAttribute.Src, imgLink);
            writer.RenderBeginTag(HtmlTextWriterTag.Img);
            writer.RenderEndTag();
            writer.RenderEndTag();
            writer.RenderEndTag();
        }

        private void AddHours(HtmlTextWriter writer, string hours)
        {
            if (!string.IsNullOrEmpty(hours))
            {
                writer.Write(hours.Replace(Constants.NewLine, Constants.Break));
            }
        }

        private void AddEmailBtn(HtmlTextWriter writer, string email)
        {
            AddBtn(writer, "Email", "mailto:" + email, Constants.EmailIcon, "insetBtn", "a", string.Empty);
        }

        private void AddFacebookBtn(HtmlTextWriter writer, string link)
        {
            AddBtn(writer, "Facebook", Utilities.AddHttpPrefix(link), Constants.FacebookIcon, "insetBtn", "f", Constants.BlankTarget);
        }

        private void AddTwitterBtn(HtmlTextWriter writer, string username)
        {
            AddBtn(writer, "Twitter", Utilities.AddTwitterPrefix(username), Constants.TwitterIcon, "insetBtn", "t", Constants.BlankTarget);
        }

        private void AddYelpBtn(HtmlTextWriter writer, string link)
        {
            AddBtn(writer, "Yelp", Utilities.AddHttpPrefix(link), Constants.YelpIcon, "insetBtn", "y", Constants.BlankTarget);
        }

        #endregion private members
    }
}