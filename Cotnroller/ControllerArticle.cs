using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebservicesSage.Singleton;
using WebservicesSage.Object;
using WebservicesSage.Object.Search;
using Objets100cLib;
using System.Windows.Forms;
using WebservicesSage.Utils;
using WebservicesSage.Utils.Enums;
using System.Timers;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using Attribute = WebservicesSage.Object.Attribute.Attribute;
using System.Data.SqlClient;

namespace WebservicesSage.Cotnroller
{
    class ControllerArticle
    {

        public static void LaunchService()
        {/*
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Elapsed += new ElapsedEventHandler(SendStockCrone);
            timer.Interval = UtilsConfig.CronTaskStock;
            timer.Enabled = true;*/
            /*System.Timers.Timer timerUpdateStatut = new System.Timers.Timer();
            timerUpdateStatut.Elapsed += new ElapsedEventHandler(SendAllProductsCron);
            timerUpdateStatut.Interval = 20000;
            timerUpdateStatut.Enabled = true;*/
            System.Timers.Timer timerCheckForUpdatedProductCron = new System.Timers.Timer();
            //timerCheckForUpdatedProductCron.Elapsed += new ElapsedEventHandler(checkForUpdatedStockProductCron);
            timerCheckForUpdatedProductCron.Interval = UtilsConfig.CronTaskUpdateStatut;
            timerCheckForUpdatedProductCron.Enabled = true;
        }

        public static void SendAllArticlesPriceCron()
        {
            var gescom = SingletonConnection.Instance.Gescom;
            string reference = "";
            string JsonErr = "";
            try
            {
                foreach (IBOArticle3 articleSageObj in gescom.FactoryArticle.List)
                {
                    if (!articleSageObj.AR_Publie)
                    {
                        continue;
                    }
                    else
                    {
                        //reference = articleSageObj.AR_Ref;
                        try
                        {
                            List<ArticleNomenclature> ArticleNomenclature = new List<ArticleNomenclature>();
                            
                            Product product = new Product();
                            Article article;

                           
                            //var articletest = articleSageObj;

                            if (!articleSageObj.AR_Publie)
                            {
                                return;
                            }
                            else
                            {
                                string RefErr = articleSageObj.AR_Ref;
                                try
                                {

                                    
                                    ProductSearchCriteria productMagento = UtilsWebservices.GetMagentoProduct("rest/V1/products", UtilsWebservices.SearchCriteria("sku", RefErr, "eq"));

                                    if (productMagento.Items.Count > 0)
                                    {
                                        article = new Article(articleSageObj);
                                        string articleXML = UtilsSerialize.SerializeObject<Article>(article);
                                        if (productMagento.TotalCount > 0)
                                        {
                                            foreach (TiersPrice item in productMagento.Items[0].TierPrices)
                                            {
                                                string deleteResponse = UtilsWebservices.SendDataJson("", @"/rest/V1/products/" + productMagento.Items[0].Sku + "/group-prices/" + item.CustomerGroupId + "/tiers/" + item.Qty + "", "DELETE");
                                                File.AppendAllText("Log\\price.txt", "supresison du global price :" + productMagento.Items[0].Sku + " , group-prices id: " + item.CustomerGroupId + " ,qty : " + item.Qty + Environment.NewLine);
                                            }
                                        }
                                        if (article.conditionnements.Count > 0)
                                        {
                                            foreach (Conditionnement item in article.conditionnements)
                                            {
                                                ProductSearchCriteria productMagentoCond = UtilsWebservices.GetMagentoProduct("rest/V1/products", UtilsWebservices.SearchCriteria("sku", article.Reference, "eq"));
                                                var jsonC = JsonConvert.SerializeObject(product.SimpleConditionnementProductPriceCronjson(article, item, productMagentoCond));
                                                File.AppendAllText("Log\\price.txt", jsonC.ToString() + Environment.NewLine);
                                                string responseC = UtilsWebservices.SendDataJson(jsonC, @"rest/V1/products/");

                                            }
                                            foreach (PrixRemiseClient item in article.prixRemisesClient)
                                            {
                                                if (item.reduction_type.Equals("amount"))
                                                {
                                                    var json1 = JsonConvert.SerializeObject(product.PrixRemise(article, item));
                                                    File.AppendAllText("Log\\price.txt", " price remise :" + json1.ToString() + Environment.NewLine);
                                                    if (!String.IsNullOrEmpty(json1))
                                                    {
                                                        string price = item.Price.ToString();
                                                        price = price.Replace(',', '.');
                                                        string response1 = UtilsWebservices.SendDataJson(json1, @"rest/all/V1/products/" + article.Reference + "/group-prices/all/tiers/" + item.Born_Sup + "/price/" + price);
                                                    }
                                                }
                                                else
                                                {
                                                    var json1 = JsonConvert.SerializeObject(product.PrixRemisePercentage(article, item));
                                                    File.AppendAllText("Log\\price.txt", " price remise :" + json1.ToString() + Environment.NewLine);
                                                    if (!String.IsNullOrEmpty(json1))
                                                    {
                                                        string response1 = UtilsWebservices.SendDataJson(json1, @"/rest/all/V1/products/tier-prices");
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (article.isGamme)
                                            {
                                                var json = JsonConvert.SerializeObject(product.ConfigurableProductPriceCronjson(article, productMagento));
                                                File.AppendAllText("Log\\price.txt", json.ToString() + Environment.NewLine);
                                                string response = UtilsWebservices.SendDataJson(json, "/rest/V1/products");
                                                if (article.IsDoubleGamme)
                                                {
                                                    UtilsWebservices.CreateDoublesGammeProduct(article);
                                                }
                                                else
                                                {
                                                    UtilsWebservices.CreateGammesProduct(article);
                                                }
                                            }
                                            else
                                            {
                                                var json = JsonConvert.SerializeObject(product.SimpleProductPriceCronjson(article, null, null, null, productMagento));
                                                File.AppendAllText("Log\\price.txt", json.ToString() + Environment.NewLine);
                                                string response = UtilsWebservices.SendDataJson(json, @"rest/V1/products/");

                                                foreach (PrixRemiseClient item in article.prixRemisesClient)
                                                {
                                                    if (item.reduction_type.Equals("amount"))
                                                    {
                                                        var json1 = JsonConvert.SerializeObject(product.PrixRemise(article, item));
                                                        File.AppendAllText("Log\\price.txt", " price remise :" + json1.ToString() + Environment.NewLine);
                                                        if (!String.IsNullOrEmpty(json1))
                                                        {
                                                            string price = item.Price.ToString();
                                                            price = price.Replace(',', '.');
                                                            string response1 = UtilsWebservices.SendDataJson(json1, @"rest/all/V1/products/" + article.Reference + "/group-prices/all/tiers/" + item.Born_Sup + "/price/" + price);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        var json1 = JsonConvert.SerializeObject(product.PrixRemisePercentage(article, item));
                                                        File.AppendAllText("Log\\price.txt", " price remise :" + json1.ToString() + Environment.NewLine);
                                                        if (!String.IsNullOrEmpty(json1))
                                                        {
                                                            string response1 = UtilsWebservices.SendDataJson(json1, @"/rest/all/V1/products/tier-prices");
                                                        }
                                                    }
                                                }

                                            }

                                        }

                                    }

                                }
                                catch (Exception s)
                                {
                                    StringBuilder sb = new StringBuilder();
                                    sb.Append(DateTime.Now + RefErr + Environment.NewLine);
                                    sb.Append(DateTime.Now + s.Message + Environment.NewLine);
                                    sb.Append(DateTime.Now + s.StackTrace + Environment.NewLine);
                                    File.AppendAllText("Log\\SyncAll.txt", sb.ToString());
                                    sb.Clear();
                                }
                            }
                        }
                        catch (Exception s)
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append(DateTime.Now + reference + Environment.NewLine);
                            sb.Append(DateTime.Now + s.Message + Environment.NewLine);
                            sb.Append(DateTime.Now + s.StackTrace + Environment.NewLine);
                            File.AppendAllText("Log\\SyncAll.txt", sb.ToString());
                            sb.Clear();
                        }
                    }
                    // SingletonUI.Instance.ProgressBar.Invoke((MethodInvoker)(() => SingletonUI.Instance.ProgressBar.Value = 100));

                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        public static void checkForUpdatedStockProductCron(object source, ElapsedEventArgs e)
        {
            DateTime d = DateTime.Now;
            d = d.AddHours(-4);
            string date = d.Year.ToString() + "-" + d.Day.ToString() + "-" + d.Month.ToString() + " " + d.Hour.ToString() + ":" + d.Minute.ToString() + ":" + d.Second.ToString() + "." + d.Millisecond.ToString();
            try
            {
                string sql = "SELECT AR_Ref FROM [" + System.Configuration.ConfigurationManager.AppSettings["DBNAME"].ToString() + "].[dbo].[F_ARTSTOCK] where CBModification >= '" + date + "'";
                SqlDataReader Articles = DB.Select(sql);

                while (Articles.Read())
                {
                    SendCustomStockArticlesCron(Articles.GetValue(0).ToString());
                    File.AppendAllText("Log\\data.txt", Environment.NewLine + DateTime.Now + " Article ref: "+ Articles.GetValue(0).ToString());
                }

                DB.Disconnect();
                SendAllArticlesPriceCron();

            }
            catch (Exception s)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now  + Environment.NewLine);
                sb.Append(DateTime.Now + s.Message + Environment.NewLine);
                sb.Append(DateTime.Now + s.StackTrace + Environment.NewLine);
                File.AppendAllText("Log\\SyncAll.txt", sb.ToString());
                sb.Clear();
            }

        }

        public static void SendCustomStockArticlesCron(string reference)
        {
            try
            {
                List<ArticleNomenclature> ArticleNomenclature = new List<ArticleNomenclature>();
                var gescom = SingletonConnection.Instance.Gescom;
                Product product = new Product();
                Article article;

                var articleSageObj = gescom.FactoryArticle.ReadReference(reference);
                //var articletest = articleSageObj;

                if (!articleSageObj.AR_Publie)
                {
                    return;
                }
                else
                {
                    string RefErr = articleSageObj.AR_Ref;
                    try
                    {

                        article = new Article(articleSageObj);
                        ProductSearchCriteria productMagento = UtilsWebservices.GetMagentoProduct("rest/V1/products", UtilsWebservices.SearchCriteria("sku", article.Reference, "eq"));

                        if(productMagento.Items.Count > 0)
                        {
                            string articleXML = UtilsSerialize.SerializeObject<Article>(article);
                            if (productMagento.TotalCount > 0)
                            {
                                foreach (TiersPrice item in productMagento.Items[0].TierPrices)
                                {
                                    string deleteResponse = UtilsWebservices.SendDataJson("", @"/rest/V1/products/" + productMagento.Items[0].Sku + "/group-prices/" + item.CustomerGroupId + "/tiers/" + item.Qty + "", "DELETE");
                                    File.AppendAllText("Log\\data.txt", "supresison du global price :" + productMagento.Items[0].Sku + " , group-prices id: " + item.CustomerGroupId + " ,qty : " + item.Qty + Environment.NewLine);
                                }
                            }
                            if (article.conditionnements.Count > 0)
                            {
                                foreach (Conditionnement item in article.conditionnements)
                                {
                                    ProductSearchCriteria productMagentoCond = UtilsWebservices.GetMagentoProduct("rest/V1/products", UtilsWebservices.SearchCriteria("sku", article.Reference, "eq"));
                                    var jsonC = JsonConvert.SerializeObject(product.SimpleConditionnementProductCronjson(article, item, productMagentoCond));
                                    File.AppendAllText("Log\\data.txt", jsonC.ToString() + Environment.NewLine);
                                    string responseC = UtilsWebservices.SendDataJson(jsonC, @"rest/V1/products/");

                                }
                                foreach (PrixRemiseClient item in article.prixRemisesClient)
                                {
                                    if (item.reduction_type.Equals("amount"))
                                    {
                                        var json1 = JsonConvert.SerializeObject(product.PrixRemise(article, item));
                                        File.AppendAllText("Log\\data.txt", " price remise :" + json1.ToString() + Environment.NewLine);
                                        if (!String.IsNullOrEmpty(json1))
                                        {
                                            string price = item.Price.ToString();
                                            price = price.Replace(',', '.');
                                            string response1 = UtilsWebservices.SendDataJson(json1, @"rest/all/V1/products/" + article.Reference + "/group-prices/all/tiers/" + item.Born_Sup + "/price/" + price);
                                        }
                                    }
                                    else
                                    {
                                        var json1 = JsonConvert.SerializeObject(product.PrixRemisePercentage(article, item));
                                        File.AppendAllText("Log\\data.txt", " price remise :" + json1.ToString() + Environment.NewLine);
                                        if (!String.IsNullOrEmpty(json1))
                                        {
                                            string response1 = UtilsWebservices.SendDataJson(json1, @"/rest/all/V1/products/tier-prices");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (article.isGamme)
                                {
                                    var json = JsonConvert.SerializeObject(product.ConfigurableProductCronjson(article, productMagento));
                                    File.AppendAllText("Log\\data.txt", json.ToString() + Environment.NewLine);
                                    string response = UtilsWebservices.SendDataJson(json, "/rest/V1/products");
                                    if (article.IsDoubleGamme)
                                    {
                                        UtilsWebservices.CreateDoublesGammeProduct(article);
                                    }
                                    else
                                    {
                                        UtilsWebservices.CreateGammesProduct(article);
                                    }
                                }
                                else
                                {
                                    var json = JsonConvert.SerializeObject(product.SimpleProductCronjson(article, null, null, null, productMagento));
                                    File.AppendAllText("Log\\data.txt", json.ToString() + Environment.NewLine);
                                    string response = UtilsWebservices.SendDataJson(json, @"rest/V1/products/");

                                    foreach (PrixRemiseClient item in article.prixRemisesClient)
                                    {
                                        if (item.reduction_type.Equals("amount"))
                                        {
                                            var json1 = JsonConvert.SerializeObject(product.PrixRemise(article, item));
                                            File.AppendAllText("Log\\data.txt", " price remise :" + json1.ToString() + Environment.NewLine);
                                            if (!String.IsNullOrEmpty(json1))
                                            {
                                                string price = item.Price.ToString();
                                                price = price.Replace(',', '.');
                                                string response1 = UtilsWebservices.SendDataJson(json1, @"rest/all/V1/products/" + article.Reference + "/group-prices/all/tiers/" + item.Born_Sup + "/price/" + price);
                                            }
                                        }
                                        else
                                        {
                                            var json1 = JsonConvert.SerializeObject(product.PrixRemisePercentage(article, item));
                                            File.AppendAllText("Log\\data.txt", " price remise :" + json1.ToString() + Environment.NewLine);
                                            if (!String.IsNullOrEmpty(json1))
                                            {
                                                string response1 = UtilsWebservices.SendDataJson(json1, @"/rest/all/V1/products/tier-prices");
                                            }
                                        }
                                    }

                                }

                            }

                        }
                        
                    }
                    catch (Exception s)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(DateTime.Now + RefErr + Environment.NewLine);
                        sb.Append(DateTime.Now + s.Message + Environment.NewLine);
                        sb.Append(DateTime.Now + s.StackTrace + Environment.NewLine);
                        File.AppendAllText("Log\\SyncAll.txt", sb.ToString());
                        sb.Clear();
                    }
                }
            }
            catch (Exception s)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now + Environment.NewLine);
                sb.Append(DateTime.Now + s.Message + Environment.NewLine);
                sb.Append(DateTime.Now + s.StackTrace + Environment.NewLine);
                File.AppendAllText("Log\\SyncAll.txt", sb.ToString());
                sb.Clear();
            }

        }

        /// <summary>
        /// Permets de remonter toute la base articles de SAGE vers Prestashop
        /// Ne remonte que les articles coché en publier sur le site marchand !
        /// </summary>
        public static void SendAllArticles()
        {
            var gescom = SingletonConnection.Instance.Gescom;
            string RefErr = "";
            string JsonErr = "";
            try
            {
                foreach (IBOArticle3 articleSageObj in gescom.FactoryArticle.List)
                {
                    if (!articleSageObj.AR_Publie)
                    {
                        continue;
                    }
                    else
                    {
                        RefErr = articleSageObj.AR_Ref;
                        try
                        {


                            List<ArticleNomenclature> ArticleNomenclature = new List<ArticleNomenclature>();
                            Product product = new Product();
                            Article article;
                            article = new Article(articleSageObj);
                            ProductSearchCriteria productMagento = UtilsWebservices.GetMagentoProduct("rest/V1/products", UtilsWebservices.SearchCriteria("sku", article.Reference, "eq"));

                            string articleXML = UtilsSerialize.SerializeObject<Article>(article);
                            if (productMagento.TotalCount > 0)
                            {
                                foreach (TiersPrice item in productMagento.Items[0].TierPrices)
                                {
                                    string deleteResponse = UtilsWebservices.SendDataJson("", @"/rest/V1/products/" + productMagento.Items[0].Sku + "/group-prices/" + item.CustomerGroupId + "/tiers/" + item.Qty + "", "DELETE");
                                    File.AppendAllText("Log\\data.txt", "supresison du global price :" + productMagento.Items[0].Sku + " , group-prices id: " + item.CustomerGroupId + " ,qty : " + item.Qty + Environment.NewLine);
                                }
                            }
                            if (article.conditionnements.Count > 0 )
                            {
                                foreach (Conditionnement item in article.conditionnements)
                                {
                                    ProductSearchCriteria productMagentoCond = UtilsWebservices.GetMagentoProduct("rest/V1/products", UtilsWebservices.SearchCriteria("sku", article.Reference, "eq"));
                                    var jsonC = JsonConvert.SerializeObject(product.SimpleConditionnementProductjson(article, item, productMagentoCond));
                                    string responseC = UtilsWebservices.SendDataJson(jsonC, @"rest/V1/products/");

                                }
                                foreach (PrixRemiseClient item in article.prixRemisesClient)
                                {
                                    if (item.reduction_type.Equals("amount"))
                                    {
                                        var json1 = JsonConvert.SerializeObject(product.PrixRemise(article, item));
                                        File.AppendAllText("Log\\data.txt", " price remise :" + json1.ToString() + Environment.NewLine);
                                        if (!String.IsNullOrEmpty(json1))
                                        {
                                            string price = item.Price.ToString();
                                            price = price.Replace(',', '.');
                                            string response1 = UtilsWebservices.SendDataJson(json1, @"rest/all/V1/products/" + article.Reference + "/group-prices/all/tiers/" + item.Born_Sup + "/price/" + price);
                                        }
                                    }
                                    else
                                    {
                                        var json1 = JsonConvert.SerializeObject(product.PrixRemisePercentage(article, item));
                                        File.AppendAllText("Log\\data.txt", " price remise :" + json1.ToString() + Environment.NewLine);
                                        if (!String.IsNullOrEmpty(json1))
                                        {
                                            string response1 = UtilsWebservices.SendDataJson(json1, @"/rest/all/V1/products/tier-prices");
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (article.isGamme)
                                {
                                    var json = JsonConvert.SerializeObject(product.ConfigurableProductjson(article, productMagento));
                                    File.AppendAllText("Log\\data.txt", json.ToString() + Environment.NewLine);
                                    string response = UtilsWebservices.SendDataJson(json, "/rest/V1/products");
                                    if (article.IsDoubleGamme)
                                    {
                                        UtilsWebservices.CreateDoublesGammeProduct(article);
                                    }
                                    else
                                    {
                                        UtilsWebservices.CreateGammesProduct(article);
                                    }
                                }
                                else
                                {
                                    var json = JsonConvert.SerializeObject(product.SimpleProductjson(article, null, null, null, productMagento));
                                    File.AppendAllText("Log\\data.txt", json.ToString() + Environment.NewLine);
                                    string response = UtilsWebservices.SendDataJson(json, @"rest/all/V1/products/");

                                    foreach (PrixClientTarif item in article.prixClientTarif)
                                    {
                                        if (item.Price != 0)
                                        {
                                            var json1 = JsonConvert.SerializeObject(product.PrixRemiseClientTarif(article, item));
                                            File.AppendAllText("Log\\data.txt", " price remise :" + json1.ToString() + Environment.NewLine);
                                            if (!String.IsNullOrEmpty(json1))
                                            {
                                                string price = item.Price.ToString();
                                                price = price.Replace(',', '.');
                                                string response1 = UtilsWebservices.SendDataJson(json1, @"rest/all/V1/products/" + article.Reference + "/group-prices/all/tiers/1/price/" + price);
                                            }
                                        }
                                    }

                                    /*foreach (PrixRemiseClient item in article.prixRemisesClient)
                                    {
                                        if (item.reduction_type.Equals("amount"))
                                        {
                                            var json1 = JsonConvert.SerializeObject(product.PrixRemise(article, item));
                                            File.AppendAllText("Log\\data.txt", " price remise :" + json1.ToString() + Environment.NewLine);
                                            if (!String.IsNullOrEmpty(json1))
                                            {
                                                string price = item.Price.ToString();
                                                price = price.Replace(',', '.');
                                                string response1 = UtilsWebservices.SendDataJson(json1, @"rest/all/V1/products/" + article.Reference + "/group-prices/all/tiers/" + item.Born_Sup + "/price/" + price);
                                            }
                                        }
                                        else
                                        {
                                            var json1 = JsonConvert.SerializeObject(product.PrixRemisePercentage(article, item));
                                            File.AppendAllText("Log\\data.txt", " price remise :" + json1.ToString() + Environment.NewLine);
                                            if (!String.IsNullOrEmpty(json1))
                                            {
                                                string response1 = UtilsWebservices.SendDataJson(json1, @"/rest/all/V1/products/tier-prices");
                                            }
                                        }
                                    }*/

                                }

                            }
                        }
                        catch (Exception s)
                        {
                            StringBuilder sb = new StringBuilder();
                            sb.Append(DateTime.Now + RefErr + Environment.NewLine);
                            sb.Append(DateTime.Now + s.Message + Environment.NewLine);
                            sb.Append(DateTime.Now + s.StackTrace + Environment.NewLine);
                            File.AppendAllText("Log\\SyncAll.txt", sb.ToString());
                            sb.Clear();
                        }
                    }
                    // SingletonUI.Instance.ProgressBar.Invoke((MethodInvoker)(() => SingletonUI.Instance.ProgressBar.Value = 100));

                }
                MessageBox.Show("end sync", "end",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        public static void SendCustomArticles(string reference)
        {
            try
            {
                List<ArticleNomenclature> ArticleNomenclature = new List<ArticleNomenclature>();
                var gescom = SingletonConnection.Instance.Gescom;
                Product product = new Product();
                Article article;

                var articleSageObj = gescom.FactoryArticle.ReadReference(reference);
                //var articletest = articleSageObj;

                if (!articleSageObj.AR_Publie)
                {
                    return;
                }
                else
                {
                    string RefErr = articleSageObj.AR_Ref;
                    try
                    {

                        article = new Article(articleSageObj);
                        ProductSearchCriteria productMagento = UtilsWebservices.GetMagentoProduct("rest/V1/products", UtilsWebservices.SearchCriteria("sku", article.Reference, "eq"));

                        string articleXML = UtilsSerialize.SerializeObject<Article>(article);
                        if (productMagento.TotalCount > 0)
                        {
                            foreach (TiersPrice item in productMagento.Items[0].TierPrices)
                            {
                                string deleteResponse = UtilsWebservices.SendDataJson("", @"/rest/V1/products/" + productMagento.Items[0].Sku + "/group-prices/" + item.CustomerGroupId + "/tiers/" + item.Qty + "", "DELETE");
                                File.AppendAllText("Log\\data.txt", "supresison du global price :" + productMagento.Items[0].Sku + " , group-prices id: " + item.CustomerGroupId + " ,qty : " + item.Qty + Environment.NewLine);
                            }
                        }
                        if (article.conditionnements.Count > 0)
                        {
                            foreach (Conditionnement item in article.conditionnements)
                            {
                                ProductSearchCriteria productMagentoCond = UtilsWebservices.GetMagentoProduct("rest/V1/products", UtilsWebservices.SearchCriteria("sku", article.Reference, "eq"));
                                var jsonC = JsonConvert.SerializeObject(product.SimpleConditionnementProductjson(article, item, productMagentoCond));

                                string responseC = UtilsWebservices.SendDataJson(jsonC, @"rest/V1/products/");

                            }
                            foreach (PrixRemiseClient item in article.prixRemisesClient)
                            {
                                if (item.reduction_type.Equals("amount"))
                                {
                                    var json1 = JsonConvert.SerializeObject(product.PrixRemise(article, item));
                                    File.AppendAllText("Log\\data.txt", " price remise :" + json1.ToString() + Environment.NewLine);
                                    if (!String.IsNullOrEmpty(json1))
                                    {
                                        string price = item.Price.ToString();
                                        price = price.Replace(',', '.');
                                        string response1 = UtilsWebservices.SendDataJson(json1, @"rest/all/V1/products/" + article.Reference + "/group-prices/all/tiers/" + item.Born_Sup + "/price/" + price);
                                    }
                                }
                                else
                                {
                                    var json1 = JsonConvert.SerializeObject(product.PrixRemisePercentage(article, item));
                                    File.AppendAllText("Log\\data.txt", " price remise :" + json1.ToString() + Environment.NewLine);
                                    if (!String.IsNullOrEmpty(json1))
                                    {
                                        string response1 = UtilsWebservices.SendDataJson(json1, @"/rest/all/V1/products/tier-prices");
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (article.isGamme)
                            {
                                var json = JsonConvert.SerializeObject(product.ConfigurableProductjson(article, productMagento));
                                File.AppendAllText("Log\\data.txt", json.ToString() + Environment.NewLine);
                                string response = UtilsWebservices.SendDataJson(json, "/rest/V1/products");
                                if (article.IsDoubleGamme)
                                {
                                    UtilsWebservices.CreateDoublesGammeProduct(article);
                                }
                                else
                                {
                                    UtilsWebservices.CreateGammesProduct(article);
                                }
                            }
                            else
                            {
                                var json = JsonConvert.SerializeObject(product.SimpleProductjson(article, null, null, null, productMagento));
                                File.AppendAllText("Log\\data.txt", json.ToString() + Environment.NewLine);
                                string response = UtilsWebservices.SendDataJson(json, @"rest/all/V1/products/");


                                foreach(PrixClientTarif item in article.prixClientTarif)
                                {
                                    if(item.Price != 0)
                                    {
                                        var json1 = JsonConvert.SerializeObject(product.PrixRemiseClientTarif(article, item));
                                        File.AppendAllText("Log\\data.txt", " price remise :" + json1.ToString() + Environment.NewLine);
                                        if (!String.IsNullOrEmpty(json1))
                                        {
                                            string price = item.Price.ToString();
                                            price = price.Replace(',', '.');
                                            string response1 = UtilsWebservices.SendDataJson(json1, @"rest/all/V1/products/" + article.Reference + "/group-prices/all/tiers/1/price/" + price);
                                        }
                                    }
                                }

                                /*foreach (PrixRemiseClient item in article.prixRemisesClient)
                                {
                                    if (item.reduction_type.Equals("amount"))
                                    {
                                        var json1 = JsonConvert.SerializeObject(product.PrixRemise(article, item));
                                        File.AppendAllText("Log\\data.txt", " price remise :" + json1.ToString() + Environment.NewLine);
                                        if (!String.IsNullOrEmpty(json1))
                                        {
                                            string price = item.Price.ToString();
                                            price = price.Replace(',', '.');
                                            string response1 = UtilsWebservices.SendDataJson(json1, @"rest/all/V1/products/" + article.Reference + "/group-prices/all/tiers/" + item.Born_Sup + "/price/" + price);
                                        }
                                    }
                                    else
                                    {
                                        var json1 = JsonConvert.SerializeObject(product.PrixRemisePercentage(article, item));
                                        File.AppendAllText("Log\\data.txt", " price remise :" + json1.ToString() + Environment.NewLine);
                                        if (!String.IsNullOrEmpty(json1))
                                        {
                                            string response1 = UtilsWebservices.SendDataJson(json1, @"/rest/all/V1/products/tier-prices");
                                        }
                                    }
                                }*/

                            }

                        }
                    }
                    catch (Exception s)
                    {
                        StringBuilder sb = new StringBuilder();
                        sb.Append(DateTime.Now + RefErr + Environment.NewLine);
                        sb.Append(DateTime.Now + s.Message + Environment.NewLine);
                        sb.Append(DateTime.Now + s.StackTrace + Environment.NewLine);
                        File.AppendAllText("Log\\CreateClient.txt", sb.ToString());
                        sb.Clear();
                    }
                }



                MessageBox.Show("end sync", "end",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
             
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message, "error",
                                 MessageBoxButtons.OK,
                                 MessageBoxIcon.Error);
            }
           
        }

        /// <summary>
        /// Permets de récupérer une liste d'articles propre depuis une liste d'artivcle SAGE
        /// Permets de gérer la configuration des produits
        /// </summary>
        /// <param name="articleSageObj">Liste d'article SAGE</param>
        /// <returns></returns>
        public static List<Article> GetListOfClientToProcess(IBICollection articleSageObj)
        {
            List<Article> articleToProcess = new List<Article>();
            string CurrentRefArticle = "";

                int incre = 0;
                foreach (IBOArticle3 articleSage in articleSageObj)
                {
                    CurrentRefArticle = articleSage.AR_Ref;
                    try
                    {
                        SingletonUI.Instance.ArticleNumber.Invoke((MethodInvoker)(() => SingletonUI.Instance.ArticleNumber.Text = "Fetching Data : " + incre));

                        // on check si l'article est cocher en publier sur le site marchand
                        if (!articleSage.AR_Publie)
                            continue;

                        Article article = new Article(articleSage);

                        if (!HandleArticleError(article))
                        {
                            articleToProcess.Add(article);
                        }
                    }
                    catch (Exception e)
                    {
                    UtilsMail.SendErrorMail(DateTime.Now + e.Message + Environment.NewLine + e.StackTrace + Environment.NewLine, "ARTICLE "+ CurrentRefArticle);
                    }
                incre++;
                }
            return articleToProcess;
        }

        /// <summary>
        /// Permet de vérifier si un article comporte des erreur ou non
        /// </summary>
        /// <param name="article">Article à tester</param>
        /// <returns></returns>
        private static bool HandleArticleError(Article article)
        {

            return false;
        }

        /// <summary>
        /// Permet de récupérer l'énuméré SAGE 1 d'un article 
        /// </summary>
        /// <param name="article"></param>
        /// <param name="gamme">Gamme sur laquelle nous devont chercher l'énuméré</param>
        /// <returns></returns>
        public static IBOArticleGammeEnum3 GetArticleGammeEnum1(IBOArticle3 article, Gamme gamme)
        {
            foreach(IBOArticleGammeEnum3 articleEnum in article.FactoryArticleGammeEnum1.List)
            {
                if (articleEnum.EG_Enumere.Equals(gamme.Value_Intitule))
                {
                    return articleEnum;
                }
            }

            return null;
        }
        public static IBOArticleCond3 GetArticleConditionnementEnum(IBOArticle3 article)
        {
            foreach (IBOArticleCond3 articleEnum in article.FactoryArticleCond.List)
            {
                if (!String.IsNullOrEmpty(articleEnum.EC_Enumere))
                {
                    return articleEnum;
                }
            }

            return null;
        }

        /// <summary>
        /// Permet de récupérer l'énuméré SAGE 2 d'un article 
        /// </summary>
        /// <param name="article"></param>
        /// <param name="gamme">Gamme sur laquelle nous devont chercher l'énuméré</param>
        /// <returns></returns>
        public static IBOArticleGammeEnum3 GetArticleGammeEnum2(IBOArticle3 article, Gamme gamme)
        {
            foreach (IBOArticleGammeEnum3 articleEnum in article.FactoryArticleGammeEnum1.List)
            {
                foreach (IBOArticleGammeEnumRef3 articleEnum2 in articleEnum.FactoryArticleGammeEnumRef.List)
                {
                    if (articleEnum.EG_Enumere.Equals(gamme.Value_Intitule) && articleEnum2.ArticleGammeEnum2.EG_Enumere.Equals(gamme.Value_Intitule2))
                    {
                        return articleEnum2.ArticleGammeEnum2;
                    }
                }
               
            }

            return null;
        }

        public static void SendStockCrone()
        {
            string currentArticleRef = "";
            try
            {
                var gescom = SingletonConnection.Instance.Gescom;
                var articleSageObj = gescom.FactoryArticle.List;
                var articles = GetListOfClientToProcess(articleSageObj);

                int increm = 1;
                int tmpiter = articles.Count % 9;
                int iter = (articles.Count - tmpiter) / 9;

                foreach (Article article in articles)
                {
                    currentArticleRef = article.Reference;
                    SingletonUI.Instance.ArticleNumber.Invoke((MethodInvoker)(() => SingletonUI.Instance.ArticleNumber.Text = "Sending data : " + increm));

                    
                    string articleXML = UtilsSerialize.SerializeObject<Article>(article);
                    UtilsWebservices.SendData(UtilsConfig.BaseUrl + EnumEndPoint.Stock.Value, articleXML);
                    increm++;
                }

               // SingletonUI.Instance.ProgressBar.Invoke((MethodInvoker)(() => SingletonUI.Instance.ProgressBar.Value = 100));

            }
            catch (Exception t)
            {
                UtilsMail.SendErrorMail(DateTime.Now + t.Message + Environment.NewLine + t.StackTrace + Environment.NewLine, "CRON STOCK");
            }
        }

        public static void SendStock()  
        {
            string currentArticleRef = "";
            try
            {
                var gescom = SingletonConnection.Instance.Gescom;
                var articleSageObj = gescom.FactoryArticle.List;
                var articles = GetListOfClientToProcess(articleSageObj);

                int increm = 1;
                int tmpiter = articles.Count % 9;
                int iter = (articles.Count - tmpiter) / 9;

                foreach (Article article in articles)
                {
                    currentArticleRef = article.Reference;
                    Product product = new Product();
                    SingletonUI.Instance.ArticleNumber.Invoke((MethodInvoker)(() => SingletonUI.Instance.ArticleNumber.Text = "Sending data : " + increm));

                    if (article.isGamme)
                    {

                        foreach (Gamme gamme in article.Gammes)
                        {
                            string sku = "";
                            if (gamme.Reference != null)
                            {
                                sku = gamme.Reference;
                            }
                            else
                            {
                                if (article.IsDoubleGamme)
                                {
                                    sku = gamme.Value_Intitule + gamme.Value_Intitule2;
                                }
                                else
                                {
                                    sku = gamme.Value_Intitule;
                                }
                            }

                            var json = JsonConvert.SerializeObject(product.CustomProductStock(article, gamme));
                            File.AppendAllText("Log\\data.txt", json.ToString() + Environment.NewLine);
                            ProductSearchCriteria productExist = UtilsWebservices.GetMagentoProduct(@"/rest/V1/products/", UtilsWebservices.SearchCriteria("sku", sku, "eq"));
                            if (productExist.TotalCount > 0 )
                            {
                                string response = UtilsWebservices.SendDataJson(json, "/rest/V1/products/" + sku, "PUT");
                            }
                            else
                            {
                                try
                                {
                                    if (article.isGamme)
                                    {
                                        var json1 = JsonConvert.SerializeObject(product.ConfigurableProductjson(article));
                                        string response = UtilsWebservices.SendDataJson(json1, "/rest/V1/products");
                                        if (article.IsDoubleGamme)
                                        {
                                            UtilsWebservices.CreateDoublesGammeProduct(article);
                                        }
                                        else
                                        {
                                            UtilsWebservices.CreateGammesProduct(article);
                                        }
                                    }
                                    else
                                    {
                                        var json1 = JsonConvert.SerializeObject(product.SimpleProductjson(article));
                                        File.AppendAllText("Log\\data.txt", json.ToString() + Environment.NewLine);
                                        string response = UtilsWebservices.SendDataJson(json1, @"rest/V1/products/");

                                    }
                                }
                                catch (Exception e)
                                {
                                    UtilsMail.SendErrorMail(DateTime.Now + e.Message + Environment.NewLine + e.StackTrace + Environment.NewLine, "Creation de Gamme Product : " + sku);
                                }
                            }

                        }
                    }
                    else
                    {

                        var json = JsonConvert.SerializeObject(product.CustomProductStock(article));
                        //ProductSearchCriteria productExist = UtilsWebservices.GetMagentoProduct(@"/rest/all/V1/products/", article.Reference);
                        ProductSearchCriteria productExist = UtilsWebservices.GetMagentoProduct(@"/rest/all/V1/products/", UtilsWebservices.SearchCriteria("sku", article.Reference, "eq"));
                        if (productExist.TotalCount > 0)
                        {
                            string response = UtilsWebservices.SendDataJson(json, "/rest/all/V1/products/" + article.Reference, "PUT");
                        }
                        else
                        {
                            try
                            {
                                var json1 = JsonConvert.SerializeObject(product.SimpleProductjson(article));
                                File.AppendAllText("Log\\data.txt", json.ToString() + Environment.NewLine);
                                string response = UtilsWebservices.SendDataJson(json1, @"rest/all/V1/products/");
                            }
                            catch (Exception e)
                            {
                                UtilsMail.SendErrorMail(DateTime.Now + e.Message + Environment.NewLine + e.StackTrace + Environment.NewLine, "Creation de Product : " + currentArticleRef);
                            }
                        }
                    }
                    
                    increm++;
                }

                //SingletonUI.Instance.ProgressBar.Invoke((MethodInvoker)(() => SingletonUI.Instance.ProgressBar.Value = 100));
                MessageBox.Show("end sync", "end",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        public static void SendCustomStock(string reference)
        {
            try
            {
                var gescom = SingletonConnection.Instance.Gescom;
                var articleSageObj = gescom.FactoryArticle.ReadReference(reference);
                Article article = new Article(articleSageObj);

                int increm = 1;

                Product product = new Product();
                SingletonUI.Instance.ArticleNumber.Invoke((MethodInvoker)(() => SingletonUI.Instance.ArticleNumber.Text = "Sending data : " + increm));
                if (article.isGamme)
                {

                    foreach (Gamme gamme in article.Gammes)
                    {
                        string sku = "";
                        if (gamme.Reference != null)
                        {
                            sku = gamme.Reference;
                        }
                        else
                        {
                            if (article.IsDoubleGamme)
                            {
                                sku = gamme.Value_Intitule + gamme.Value_Intitule2;
                            }
                            else
                            {
                                sku = gamme.Value_Intitule;
                            }
                        }

                        var json = JsonConvert.SerializeObject(product.CustomProductStock(article, gamme));
                        
                        ProductSearchCriteria productExist = UtilsWebservices.GetMagentoProduct(@"/rest/V1/products/", UtilsWebservices.SearchCriteria("sku",sku,"eq"));
                        if (productExist != null)
                        {
                            string response = UtilsWebservices.SendDataJson(json, "/rest/V1/products/" + sku, "PUT");
                        }
                        else
                        {
                            try
                            {
                                if (article.isGamme)
                                {
                                    var json1 = JsonConvert.SerializeObject(product.ConfigurableProductjson(article));
                                    
                                    string response = UtilsWebservices.SendDataJson(json1, "/rest/V1/products");
                                    if (article.IsDoubleGamme)
                                    {
                                        UtilsWebservices.CreateDoublesGammeProduct(article);
                                    }
                                    else
                                    {
                                        UtilsWebservices.CreateGammesProduct(article);
                                    }
                                }
                                else
                                {
                                    var json1 = JsonConvert.SerializeObject(product.SimpleProductjson(article));
                                    string response = UtilsWebservices.SendDataJson(json1, @"rest/V1/products/");

                                }
                            }
                            catch (Exception e)
                            {
                                UtilsMail.SendErrorMail(DateTime.Now + e.Message + Environment.NewLine + e.StackTrace + Environment.NewLine, "Creation de Gamme Product : " + reference);
                            }
                        }

                    }
                }
                else
                {

                    var json = JsonConvert.SerializeObject(product.CustomProductStock(article));
                    //ProductSearchCriteria productExist = UtilsWebservices.GetMagentoProduct(@"/rest/all/V1/products/", article.Reference);
                    ProductSearchCriteria productExist = UtilsWebservices.GetMagentoProduct(@"/rest/V1/products/", UtilsWebservices.SearchCriteria("sku", article.Reference, "eq"));
                    if (productExist.TotalCount >0)
                    {
                        string response = UtilsWebservices.SendDataJson(json, "/rest/V1/products/" + article.Reference, "PUT");
                    }
                    else
                    {
                        try
                        {
                            var json1 = JsonConvert.SerializeObject(product.SimpleProductjson(article));
                            string response = UtilsWebservices.SendDataJson(json1, @"rest/V1/products/");
                        }
                        catch (Exception e)
                        {
                            UtilsMail.SendErrorMail(DateTime.Now + e.Message + Environment.NewLine + e.StackTrace + Environment.NewLine, "Creation de Product : " + reference);
                        }
                    }

                }
                /*string articleXML = UtilsSerialize.SerializeObject<Article>(article);
                UtilsWebservices.SendData(UtilsConfig.BaseUrl + EnumEndPoint.Stock.Value, articleXML);      */

                //  SingletonUI.Instance.ProgressBar.Invoke((MethodInvoker)(() => SingletonUI.Instance.ProgressBar.Value = 100));
                MessageBox.Show("end sync", "end",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        public static void SendPrice()
        {
            try
            {
                var gescom = SingletonConnection.Instance.Gescom;
                var articleSageObj = gescom.FactoryArticle.List;
                var articles = GetListOfClientToProcess(articleSageObj);

                int increm = 1;
                int tmpiter = articles.Count % 9;
                int iter = (articles.Count - tmpiter) / 9;

                foreach (Article article in articles)
                {

                    SingletonUI.Instance.ArticleNumber.Invoke((MethodInvoker)(() => SingletonUI.Instance.ArticleNumber.Text = "Sending data : " + increm));

                    Product product = new Product();
                    if (article.isGamme)
                    {

                        foreach (Gamme gamme in article.Gammes)
                        {
                            string sku = "";
                            if (gamme.Reference != null)
                            {
                                sku = gamme.Reference;
                            }
                            else
                            {
                                if (article.IsDoubleGamme)
                                {
                                    sku = gamme.Value_Intitule + gamme.Value_Intitule2;
                                }
                                else
                                {
                                    sku = gamme.Value_Intitule;
                                }
                            }

                            var json = JsonConvert.SerializeObject(product.CustomProductPrice(article, gamme));
                            ProductSearchCriteria productExist = UtilsWebservices.GetMagentoProduct(@"/rest/V1/products/", UtilsWebservices.SearchCriteria("sku", sku, "eq"));
                            if (productExist.TotalCount > 0)
                            {
                                string response = UtilsWebservices.SendDataJson(json, "/rest/V1/products/" + sku, "PUT");
                            }
                            else
                            {
                                try
                                {
                                    if (article.isGamme)
                                    {
                                        var json1 = JsonConvert.SerializeObject(product.ConfigurableProductjson(article));
                                        string response = UtilsWebservices.SendDataJson(json1, "/rest/V1/products");
                                        if (article.IsDoubleGamme)
                                        {
                                            UtilsWebservices.CreateDoublesGammeProduct(article);
                                        }
                                        else
                                        {
                                            UtilsWebservices.CreateGammesProduct(article);
                                        }
                                    }
                                    else
                                    {
                                        var json1 = JsonConvert.SerializeObject(product.SimpleProductjson(article));
                                        string response = UtilsWebservices.SendDataJson(json1, @"rest/V1/products/");

                                    }
                                }
                                catch (Exception e)
                                {
                                    UtilsMail.SendErrorMail(DateTime.Now + e.Message + Environment.NewLine + e.StackTrace + Environment.NewLine, "Creation de Gamme Product : " + article.Reference);
                                }
                            }

                        }
                    }
                    else
                    {

                        var json = JsonConvert.SerializeObject(product.CustomProductPrice(article));
                        ProductSearchCriteria productExist = UtilsWebservices.GetMagentoProduct(@"/rest/V1/products/", UtilsWebservices.SearchCriteria("sku", article.Reference, "eq"));
                        if (productExist.TotalCount > 0)
                        {
                            string response = UtilsWebservices.SendDataJson(json, "/rest/V1/products/" + article.Reference, "PUT");
                        }
                        else
                        {
                            try
                            {
                                var json1 = JsonConvert.SerializeObject(product.SimpleProductjson(article));
                                string response = UtilsWebservices.SendDataJson(json1, @"rest/V1/products/");
                            }
                            catch (Exception e)
                            {
                                UtilsMail.SendErrorMail(DateTime.Now + e.Message + Environment.NewLine + e.StackTrace + Environment.NewLine, "Creation de Product : " + article.Reference);
                            }
                        }

                    }


                    increm++;
                }

               // SingletonUI.Instance.ProgressBar.Invoke((MethodInvoker)(() => SingletonUI.Instance.ProgressBar.Value = 100));
                MessageBox.Show("end sync", "end",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        public static void SendCustomPrice(string reference)
        {
            try
            {
                var gescom = SingletonConnection.Instance.Gescom;
                var articleSageObj = gescom.FactoryArticle.ReadReference(reference);
                Article article = new Article(articleSageObj);

                int increm = 1;

                SingletonUI.Instance.ArticleNumber.Invoke((MethodInvoker)(() => SingletonUI.Instance.ArticleNumber.Text = "Sending data : " + increm));
                Product product = new Product();
                if (article.isGamme)
                {

                    foreach (Gamme gamme in article.Gammes)
                    {
                        string sku = "";
                        if (gamme.Reference != null)
                        {
                            sku = gamme.Reference;
                        }
                        else
                        {
                            if (article.IsDoubleGamme)
                            {
                                sku = gamme.Value_Intitule + gamme.Value_Intitule2;
                            }
                            else
                            {
                                sku = gamme.Value_Intitule;
                            }
                        }

                        var json = JsonConvert.SerializeObject(product.CustomProductPrice(article, gamme));
                        ProductSearchCriteria productExist = UtilsWebservices.GetMagentoProduct(@"/rest/V1/products/", UtilsWebservices.SearchCriteria("sku", sku, "eq"));
                        if (productExist.TotalCount > 0 )
                        {
                            string response = UtilsWebservices.SendDataJson(json, "/rest/V1/products/" + sku, "PUT");
                        }
                        else
                        {
                            try
                            {
                                if (article.isGamme)
                                {
                                    var json1 = JsonConvert.SerializeObject(product.ConfigurableProductjson(article));
                                    string response = UtilsWebservices.SendDataJson(json1, "/rest/V1/products");
                                    if (article.IsDoubleGamme)
                                    {
                                        UtilsWebservices.CreateDoublesGammeProduct(article);
                                    }
                                    else
                                    {
                                        UtilsWebservices.CreateGammesProduct(article);
                                    }
                                }
                                else
                                {
                                    var json1 = JsonConvert.SerializeObject(product.SimpleProductjson(article));
                                    string response = UtilsWebservices.SendDataJson(json1, @"rest/V1/products/");

                                }
                            }
                            catch (Exception e)
                            {
                                UtilsMail.SendErrorMail(DateTime.Now + e.Message + Environment.NewLine + e.StackTrace + Environment.NewLine, "Creation de Gamme Product : " + reference);
                            }
                        }

                    }
                }
                else
                {

                    var json = JsonConvert.SerializeObject(product.CustomProductPrice(article));
                    ProductSearchCriteria productExist = UtilsWebservices.GetMagentoProduct(@"/rest/V1/products/", UtilsWebservices.SearchCriteria("sku", article.Reference, "eq"));
                    if (productExist.TotalCount > 0 )
                    {
                        string response = UtilsWebservices.SendDataJson(json, "/rest/V1/products/" + article.Reference, "PUT");
                    }
                    else
                    {
                        try
                        {
                            var json1 = JsonConvert.SerializeObject(product.SimpleProductjson(article));
                            string response = UtilsWebservices.SendDataJson(json1, @"rest/V1/products/");
                        }
                        catch (Exception e)
                        {
                            UtilsMail.SendErrorMail(DateTime.Now + e.Message + Environment.NewLine + e.StackTrace + Environment.NewLine, "Creation de Product : " + reference);
                        }
                    }

                }

                MessageBox.Show("end sync", "end",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }
    }
}







