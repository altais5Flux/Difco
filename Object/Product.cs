using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebservicesSage.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections;
using WebservicesSage.Object.Categories;
using System.IO;

namespace WebservicesSage.Object
{
    class Product
    {

        public List<int> website_ids { get; set; }
        public int status { get; set; }
        public Double price { get; set; }
        public int visibility { get; set; }
        public string type_id { get; set; }
        public string sku { get; set; }
        public string name { get; set; }
        public bool is_in_stock { get; set; }
        public double stock { get; set; }
        public List<CustomAttribute> CustomAttributes { get; set; }
        public Product()
        {

        }
        public partial class CustomAttribute
        {
            [JsonProperty("attribute_code")]
            public string AttributeCode { get; set; }

            [JsonProperty("value")]
            public object Value { get; set; }
        }
        public object GroupedProduct(Article article, Conditionnement conditionnement, ProductSearchCriteria productMagento = null)
        {
            if (productMagento.TotalCount > 0)
            {
                status = 2;
            }
            var product = new
            {
                product = new
                {
                    sku = conditionnement.Reference.ToString(),
                    name = article.Designation + conditionnement.Enumere,
                    attribute_set_id = int.Parse(UtilsConfig.Attribute_set_id.ToString()),
                    type_id = "grouped",
                    //status = status,
                    visibility = 4
                }
            };
            return product;
        }
        public object SimpleGroupedProduct(Article article, Conditionnement conditionnement)
        {

            var items = new
            {
                entity = new
                {
                    sku = conditionnement.Reference.ToString(),
                    link_type = "associated",
                    linked_product_sku = article.Reference.ToString(),
                    linked_product_type = "simple",
                    position = 0,
                    extension_attributes = new
                    {
                        qty = conditionnement.Quantity
                    }
                }
            };
            return items;
        }
        public object PrixCatTarif(Article article, PrixCatTarif prixCatTarif)
        {
            var prices = new
            {
                prices = new
                {
                    price = prixCatTarif.Price,
                    price_type = "fixed",
                    website_id = 0,
                    sku = article.Reference,
                    quantity = 1,
                    customer_groupe = prixCatTarif.CategorieTarifaire
                }
            };
            return prices;
        }
        public object PrixRemise(Article article, PrixRemiseClient prixRemise)
        {
            if (prixRemise.reduction_type.Equals("amount"))
            {
                ArrayList price = new ArrayList();
                var prices = new
                {
                    price = prixRemise.Price,
                    price_type = "fixed",
                    website_id = 0,
                    sku = article.Reference,
                    quantity = prixRemise.Born_Sup,
                    customer_group = prixRemise.ClientCtNum
                };
                price.Add(prices);
                var priceRemise = new
                {
                    prices = price
                };
                return priceRemise;
            }/*
            else
            {
                var prices = new
                {
                    prices = new
                    {
                        price = prixRemise.RemisePercentage * 100,
                        price_type = "discount",
                        website_id = 0,
                        sku = article.Reference,
                        quantity = prixRemise.Born_Sup,
                        customer_groupe = prixRemise.CategorieTarifaire
                    }
                };
                return prices;
            }*/
            return null;
        }

        public object PrixRemiseClientTarif(Article article, PrixClientTarif prixRemise)
        {
            ArrayList price = new ArrayList();
            var prices = new
            {
                price = prixRemise.Price,
                price_type = "fixed",
                website_id = 0,
                sku = article.Reference,
                quantity = "1",
                customer_group = prixRemise.ClinetCtNum
            };
            price.Add(prices);
            var priceRemise = new
            {
                prices = price
            };
            return priceRemise;
        }

        public object PrixRemisePercentage(Article article, PrixRemiseClient prixRemise)
        {
            ArrayList price = new ArrayList();
            var prices = new
            {
                price = prixRemise.RemisePercentage * 100,
                price_type = "discount",
                website_id = 0,
                sku = article.Reference,
                quantity = prixRemise.Born_Sup,
                customer_group = prixRemise.ClientCtNum
            };
            price.Add(prices);
            var priceRemise = new
            {
                prices = price
            };
            return priceRemise;
        }

        public object SimpleConditionnementProductPriceCronjson(Article article, Conditionnement conditionnement, ProductSearchCriteria productMagento = null)
        {
            //is_in_stock = false;
            CustomAttributes = new List<CustomAttribute>();

            website_ids = new List<int>();
            website_ids.Add(int.Parse(UtilsConfig.Store.ToString()));

            if (article.Sommeil)
            {
                status = 2;
            }
            else
            {
                status = 1;
            }
            if (article.Stock > 0)
            {
                is_in_stock = true;
            }
            else
            {
                is_in_stock = false;
            }
            sku = article.Reference;
            if (article.prixCatTarifs.Count > 0)
            {
                if (article.prixCatTarifs[0].Price > 0)
                {
                    price = article.prixCatTarifs[0].Price;
                }
            }
            if (!(price > 0))
            {
                price = article.PrixVente;
            }

            stock = article.Stock;
            name = article.Designation;

            if (!String.IsNullOrEmpty(article.Ecotaxe))
            {

                var value = new
                {
                    website_id = int.Parse(UtilsConfig.Store.ToString()),
                    country = "FR",
                    state = 0,
                    value = Double.Parse(article.Ecotaxe),
                    website_value = Double.Parse(article.Ecotaxe)
                };
                CustomAttribute ecotax = new CustomAttribute();
                ecotax.AttributeCode = "fpt_tax";
                ecotax.Value = value.ToString();
                CustomAttributes.Add(ecotax);
            }
            if (productMagento.TotalCount > 0)
            {
                status = 1;
            }
            else
            {
                status = 2;
            }
            if (productMagento.TotalCount > 0)
            {
                status = productMagento.Items[0].Status;
                visibility = productMagento.Items[0].Visibility;
            }
            else
            {
                status = 2;
                visibility = 1;
            }

            var product = new
            {
                product = new
                {
                    //name = name + " " + conditionnement.Enumere,
                    sku = sku,
                    price = price * int.Parse(conditionnement.Quantity),
                    //status = status,
                    //attribute_set_id = int.Parse(UtilsConfig.Attribute_set_id.ToString()),
                    //visibility = visibility,
                    type_id = "simple"
                    //weight = article.Poid * int.Parse(conditionnement.Quantity),
                    /*,
                    custom_attributes = CustomAttributes
                    /*new
                    {
                        attribute_code = custom_attribute.AttributeCode.ToString(),
                        value = custom_attribute.Value.ToString()
                    }*/
                },
                saveOptions = true

            };
            return product;
        }


        public object SimpleConditionnementProductCronjson(Article article, Conditionnement conditionnement, ProductSearchCriteria productMagento = null)
        {
            //is_in_stock = false;
            CustomAttributes = new List<CustomAttribute>();

            website_ids = new List<int>();
            website_ids.Add(int.Parse(UtilsConfig.Store.ToString()));

            if (article.Sommeil)
            {
                status = 2;
            }
            else
            {
                status = 1;
            }
            if (article.Stock > 0)
            {
                is_in_stock = true;
            }
            else
            {
                is_in_stock = false;
            }
            sku = article.Reference;
            if (article.prixCatTarifs.Count > 0)
            {
                if (article.prixCatTarifs[0].Price > 0)
                {
                    price = article.prixCatTarifs[0].Price;
                }
            }
            if (!(price > 0))
            {
                price = article.PrixVente;
            }

            stock = article.Stock;
            name = article.Designation;

            if (!String.IsNullOrEmpty(article.Ecotaxe))
            {

                var value = new
                {
                    website_id = int.Parse(UtilsConfig.Store.ToString()),
                    country = "FR",
                    state = 0,
                    value = Double.Parse(article.Ecotaxe),
                    website_value = Double.Parse(article.Ecotaxe)
                };
                CustomAttribute ecotax = new CustomAttribute();
                ecotax.AttributeCode = "fpt_tax";
                ecotax.Value = value.ToString();
                CustomAttributes.Add(ecotax);
            }
            if (productMagento.TotalCount > 0)
            {
                status = 1;
            }
            else
            {
                status = 2;
            }
            if (productMagento.TotalCount > 0)
            {
                status = productMagento.Items[0].Status;
                visibility = productMagento.Items[0].Visibility;
            }
            else
            {
                status = 2;
                visibility = 1;
            }

            var product = new
            {
                product = new
                {
                    //name = name + " " + conditionnement.Enumere,
                    sku = sku,
                    price = price * int.Parse(conditionnement.Quantity),
                    //status = status,
                    //attribute_set_id = int.Parse(UtilsConfig.Attribute_set_id.ToString()),
                    //visibility = visibility,
                    type_id = "simple",
                    //weight = article.Poid * int.Parse(conditionnement.Quantity),
                    extension_attributes = new
                    {
                        website_ids = website_ids,


                        stock_item = new
                        {
                            qty = stock,
                            //min_qty get value from infolibre
                            min_qty = 1,
                            is_in_stock = true
                        }
                    }/*,
                    custom_attributes = CustomAttributes
                    /*new
                    {
                        attribute_code = custom_attribute.AttributeCode.ToString(),
                        value = custom_attribute.Value.ToString()
                    }*/
                },
                saveOptions = true

            };
            return product;
        }

        public object SimpleConditionnementProductjson(Article article,Conditionnement conditionnement, ProductSearchCriteria productMagento = null)
        {
            //is_in_stock = false;
            CustomAttributes = new List<CustomAttribute>();

            website_ids = new List<int>();
            website_ids.Add(int.Parse(UtilsConfig.Store.ToString()));

                if (article.Sommeil)
                {
                    status = 2;
                }
                else
                {
                    status = 1;
                }
                if (article.Stock > 0)
                {
                    is_in_stock = true;
                }
                else
                {
                    is_in_stock = false;
                }
            sku = article.Reference;
                if (article.prixCatTarifs.Count > 0)
                {
                    if (article.prixCatTarifs[0].Price > 0)
                    {
                        price = article.prixCatTarifs[0].Price;
                    }
                }
                if (!(price > 0))
                {
                    price = article.PrixVente;
                }

                stock = article.Stock;
                name = article.Designation ;
            
            if (!String.IsNullOrEmpty(article.Ecotaxe))
            {

                var value = new
                {
                    website_id = int.Parse(UtilsConfig.Store.ToString()),
                    country = "FR",
                    state = 0,
                    value = Double.Parse(article.Ecotaxe),
                    website_value = Double.Parse(article.Ecotaxe)
                };
                CustomAttribute ecotax = new CustomAttribute();
                ecotax.AttributeCode = "fpt_tax";
                ecotax.Value = value.ToString();
                CustomAttributes.Add(ecotax);
            }
            if (productMagento.TotalCount > 0)
            {
                status = 1;
            }
            else
            {
                status = 2;
            }
            if (productMagento.TotalCount > 0)
            {
                status = productMagento.Items[0].Status;
                visibility = productMagento.Items[0].Visibility;
            }
            else
            {
                status = 2;
                visibility = 1;
            }

            var product = new
            {
                product = new
                {
                    name = name+" "+ conditionnement.Enumere,
                    sku = sku,
                    price = price * int.Parse(conditionnement.Quantity),
                    status = status,
                    attribute_set_id = int.Parse(UtilsConfig.Attribute_set_id.ToString()),
                    visibility = visibility,
                    type_id = "simple",
                    weight = article.Poid * int.Parse(conditionnement.Quantity),
                    extension_attributes = new
                    {
                        website_ids = website_ids,


                        stock_item = new
                        {
                            qty = stock,
                            //min_qty get value from infolibre
                            min_qty = 1,
                            is_in_stock = true
                        }
                    },
                    custom_attributes = CustomAttributes
                    /*new
                    {
                        attribute_code = custom_attribute.AttributeCode.ToString(),
                        value = custom_attribute.Value.ToString()
                    }*/
                },
                saveOptions = true

            };
            return product;
        }

        public object SimpleProductPriceCronjson(Article article, Gamme gamme = null, string value_index = null, string value_index2 = null, ProductSearchCriteria productMagento = null)
        {
            //is_in_stock = false;

            CustomAttribute custom_attribute = new CustomAttribute();
            CustomAttribute custom_attribute1 = new CustomAttribute();
            CustomAttributes = new List<CustomAttribute>();
            if (!String.IsNullOrEmpty(article.infoLibre[0].Value))
            {
                CustomAttribute infolibre1 = new CustomAttribute();
                infolibre1.AttributeCode = "ref_fournisseur";
                infolibre1.Value = article.infoLibre[0].Value;
                CustomAttributes.Add(infolibre1);
            }
            if (!String.IsNullOrEmpty(article.infoLibre[1].Value))
            {
                CustomAttribute infolibre2 = new CustomAttribute();
                infolibre2.AttributeCode = "dompro";
                infolibre2.Value = article.infoLibre[1].Value;
                CustomAttributes.Add(infolibre2);
            }

            System.IO.File.AppendAllText("Log\\product.txt", DateTime.Now + " recherche famille article : " + article.Famille + Environment.NewLine);
            CategoriesSearch categoriesSearch = CategoriesSearch.FromJson(UtilsWebservices.GetMagentoData("rest/V1/categories/list?searchCriteria[filter_groups][1][filters][0][field]=name&searchCriteria[filter_groups][1][filters][0][value]=" + article.Famille + "&searchCriteria[filter_groups][1][filters][0][condition_type]=eq"));
            //ProductCategories test = ProductCategories.FromJson(UtilsWebservices.GetMagentoData("rest/V1/categories/list?searchCriteria[filter_groups][1][filters][0][field]=name&searchCriteria[filter_groups][1][filters][0][value]="+article.Famille+"&searchCriteria[filter_groups][1][filters][0][condition_type]=eq"));
            if (categoriesSearch.TotalCount > 0)
            {
                List<int> cats = new List<int>();
                cats.Add(int.Parse(categoriesSearch.Items[0].Id.ToString()));
                CustomAttribute cat = new CustomAttribute();
                cat.AttributeCode = "category_ids";
                cat.Value = cats;
                CustomAttributes.Add(cat);
            }
            else
            {
                List<int> cats = new List<int>();
                cats.Add(int.Parse(UtilsConfig.Category.ToString()));
                CustomAttribute cat = new CustomAttribute();
                cat.AttributeCode = "category_ids";
                cat.Value = cats;
                CustomAttributes.Add(cat);
            }
            if (!String.IsNullOrEmpty(article.CodeBarres))
            {
                CustomAttribute CodeEAN = new CustomAttribute();
                CodeEAN.AttributeCode = "code_ean";
                CodeEAN.Value = article.CodeBarres;
                CustomAttributes.Add(CodeEAN);
            }
            website_ids = new List<int>();
            website_ids.Add(int.Parse(UtilsConfig.Store.ToString()));
            if (gamme != null)
            {
                custom_attribute.AttributeCode = gamme.Intitule;
                custom_attribute.Value = value_index;
                CustomAttributes.Add(custom_attribute);
                if (gamme.Reference != null)
                {
                    sku = gamme.Reference;
                    if (article.IsDoubleGamme)
                    {
                        custom_attribute1.AttributeCode = gamme.Intitule2;
                        custom_attribute1.Value = value_index2;
                        CustomAttributes.Add(custom_attribute1);
                    }
                }
                else
                {
                    if (article.IsDoubleGamme)
                    {
                        sku = gamme.Value_Intitule + gamme.Value_Intitule2;
                        custom_attribute1.AttributeCode = gamme.Intitule2;
                        custom_attribute1.Value = value_index2;
                        CustomAttributes.Add(custom_attribute1);
                    }
                    else
                    {
                        sku = gamme.Value_Intitule;
                    }
                }

                price = gamme.Price;
                stock = gamme.Stock;
                if (article.IsDoubleGamme)
                {
                    name = article.Designation + " " + gamme.Value_Intitule + " " + gamme.Value_Intitule2;
                }
                else
                {
                    name = article.Designation + " " + gamme.Value_Intitule;
                }
                if (gamme.Sommeil)
                {
                    status = 2;
                }
                else
                {
                    if (gamme.Stock > 0)
                    {
                        status = 1;
                        is_in_stock = true;
                    }
                }

            }
            else
            {
                if (article.Sommeil)
                {
                    status = 2;
                }
                else
                {
                    status = 1;
                }
                if (article.Stock > 0)
                {
                    is_in_stock = true;
                }
                else
                {
                    is_in_stock = false;
                }
                sku = article.Reference;
                /*if (article.prixCatTarifs.Count > 0)
                {
                    if (article.prixCatTarifs[0].Price > 0)
                    {
                        price = article.prixCatTarifs[0].Price;
                    }
                }
                if (!(price >0))
                {
                    price = article.PrixVente;
                }*/
                price = article.PrixVente;
                stock = article.Stock;
                name = article.Designation;
            }
            File.AppendAllText("Log\\data.txt", price.ToString() + Environment.NewLine);

            /*if (!String.IsNullOrEmpty(article.Ecotaxe))
            {

                var value = new
                {
                    website_id = int.Parse(UtilsConfig.Store.ToString()),
                    country = "FR",
                    state = 0,
                    value = Double.Parse(article.Ecotaxe),
                    website_value = Double.Parse(article.Ecotaxe)
                };
                CustomAttribute ecotax = new CustomAttribute();
                ecotax.AttributeCode = "fpt_tax";
                ecotax.Value = value.ToString();
                CustomAttributes.Add(ecotax);
            }*/
            /*if (productMagento.TotalCount > 0)
            {
                status = productMagento.Items[0].Status;
                visibility = productMagento.Items[0].Visibility;
            }
            else
            {
                status = 2;
                visibility = 1;
            }
            */
            if (productMagento.TotalCount > 0)
            {
                status = productMagento.Items[0].Status;
                visibility = productMagento.Items[0].Visibility;
            }
            else
            {
                status = 2;
                visibility = 1;
            }

            /*if (productMagento.TotalCount >0)
            {
                var product = new
                {
                    product = new
                    {
                        sku = sku,
                        price = price,
                        attribute_set_id = int.Parse(UtilsConfig.Attribute_set_id.ToString()),
                        type_id = "simple",
                        weight = article.Poid,
                        extension_attributes = new
                        {
                            website_ids = website_ids,


                            stock_item = new
                            {
                                qty = stock,
                                min_qty = 1,
                                is_in_stock = is_in_stock
                            }
                        },
                        custom_attributes = CustomAttributes

                    },
                    saveOptions = true

                };
                return product;
            }
            else
            {*/

            var product = new
            {
                product = new
                {
                    //name = name,
                    sku = sku,
                    price = price,
                    //status = status,
                    //attribute_set_id = int.Parse(UtilsConfig.Attribute_set_id.ToString()),
                    //visibility = visibility,
                    type_id = "simple"
                    //weight = article.Poid,
                    /*,
                    custom_attributes = CustomAttributes*/

                },
                saveOptions = true

            };
            return product;
            //}


        }


        public object SimpleProductCronjson(Article article, Gamme gamme = null, string value_index = null, string value_index2 = null, ProductSearchCriteria productMagento = null)
        {
            //is_in_stock = false;

            CustomAttribute custom_attribute = new CustomAttribute();
            CustomAttribute custom_attribute1 = new CustomAttribute();
            CustomAttributes = new List<CustomAttribute>();
            if (!String.IsNullOrEmpty(article.infoLibre[0].Value))
            {
                CustomAttribute infolibre1 = new CustomAttribute();
                infolibre1.AttributeCode = "ref_fournisseur";
                infolibre1.Value = article.infoLibre[0].Value;
                CustomAttributes.Add(infolibre1);
            }
            if (!String.IsNullOrEmpty(article.infoLibre[1].Value))
            {
                CustomAttribute infolibre2 = new CustomAttribute();
                infolibre2.AttributeCode = "dompro";
                infolibre2.Value = article.infoLibre[1].Value;
                CustomAttributes.Add(infolibre2);
            }

            System.IO.File.AppendAllText("Log\\product.txt", DateTime.Now + " recherche famille article : " + article.Famille + Environment.NewLine);
            CategoriesSearch categoriesSearch = CategoriesSearch.FromJson(UtilsWebservices.GetMagentoData("rest/V1/categories/list?searchCriteria[filter_groups][1][filters][0][field]=name&searchCriteria[filter_groups][1][filters][0][value]=" + article.Famille + "&searchCriteria[filter_groups][1][filters][0][condition_type]=eq"));
            //ProductCategories test = ProductCategories.FromJson(UtilsWebservices.GetMagentoData("rest/V1/categories/list?searchCriteria[filter_groups][1][filters][0][field]=name&searchCriteria[filter_groups][1][filters][0][value]="+article.Famille+"&searchCriteria[filter_groups][1][filters][0][condition_type]=eq"));
            if (categoriesSearch.TotalCount > 0)
            {
                List<int> cats = new List<int>();
                cats.Add(int.Parse(categoriesSearch.Items[0].Id.ToString()));
                CustomAttribute cat = new CustomAttribute();
                cat.AttributeCode = "category_ids";
                cat.Value = cats;
                CustomAttributes.Add(cat);
            }
            else
            {
                List<int> cats = new List<int>();
                cats.Add(int.Parse(UtilsConfig.Category.ToString()));
                CustomAttribute cat = new CustomAttribute();
                cat.AttributeCode = "category_ids";
                cat.Value = cats;
                CustomAttributes.Add(cat);
            }
            if (!String.IsNullOrEmpty(article.CodeBarres))
            {
                CustomAttribute CodeEAN = new CustomAttribute();
                CodeEAN.AttributeCode = "code_ean";
                CodeEAN.Value = article.CodeBarres;
                CustomAttributes.Add(CodeEAN);
            }
            website_ids = new List<int>();
            website_ids.Add(int.Parse(UtilsConfig.Store.ToString()));
            if (gamme != null)
            {
                custom_attribute.AttributeCode = gamme.Intitule;
                custom_attribute.Value = value_index;
                CustomAttributes.Add(custom_attribute);
                if (gamme.Reference != null)
                {
                    sku = gamme.Reference;
                    if (article.IsDoubleGamme)
                    {
                        custom_attribute1.AttributeCode = gamme.Intitule2;
                        custom_attribute1.Value = value_index2;
                        CustomAttributes.Add(custom_attribute1);
                    }
                }
                else
                {
                    if (article.IsDoubleGamme)
                    {
                        sku = gamme.Value_Intitule + gamme.Value_Intitule2;
                        custom_attribute1.AttributeCode = gamme.Intitule2;
                        custom_attribute1.Value = value_index2;
                        CustomAttributes.Add(custom_attribute1);
                    }
                    else
                    {
                        sku = gamme.Value_Intitule;
                    }
                }

                price = gamme.Price;
                stock = gamme.Stock;
                if (article.IsDoubleGamme)
                {
                    name = article.Designation + " " + gamme.Value_Intitule + " " + gamme.Value_Intitule2;
                }
                else
                {
                    name = article.Designation + " " + gamme.Value_Intitule;
                }
                if (gamme.Sommeil)
                {
                    status = 2;
                }
                else
                {
                    if (gamme.Stock > 0)
                    {
                        status = 1;
                        is_in_stock = true;
                    }
                }

            }
            else
            {
                if (article.Sommeil)
                {
                    status = 2;
                }
                else
                {
                    status = 1;
                }
                if (article.Stock > 0)
                {
                    is_in_stock = true;
                }
                else
                {
                    is_in_stock = false;
                }
                sku = article.Reference;
                /*if (article.prixCatTarifs.Count > 0)
                {
                    if (article.prixCatTarifs[0].Price > 0)
                    {
                        price = article.prixCatTarifs[0].Price;
                    }
                }
                if (!(price >0))
                {
                    price = article.PrixVente;
                }*/
                price = article.PrixVente;
                stock = article.Stock;
                name = article.Designation;
            }
            File.AppendAllText("Log\\data.txt", price.ToString() + Environment.NewLine);

            /*if (!String.IsNullOrEmpty(article.Ecotaxe))
            {

                var value = new
                {
                    website_id = int.Parse(UtilsConfig.Store.ToString()),
                    country = "FR",
                    state = 0,
                    value = Double.Parse(article.Ecotaxe),
                    website_value = Double.Parse(article.Ecotaxe)
                };
                CustomAttribute ecotax = new CustomAttribute();
                ecotax.AttributeCode = "fpt_tax";
                ecotax.Value = value.ToString();
                CustomAttributes.Add(ecotax);
            }*/
            /*if (productMagento.TotalCount > 0)
            {
                status = productMagento.Items[0].Status;
                visibility = productMagento.Items[0].Visibility;
            }
            else
            {
                status = 2;
                visibility = 1;
            }
            */
            if (productMagento.TotalCount > 0)
            {
                status = productMagento.Items[0].Status;
                visibility = productMagento.Items[0].Visibility;
            }
            else
            {
                status = 2;
                visibility = 1;
            }

            /*if (productMagento.TotalCount >0)
            {
                var product = new
                {
                    product = new
                    {
                        sku = sku,
                        price = price,
                        attribute_set_id = int.Parse(UtilsConfig.Attribute_set_id.ToString()),
                        type_id = "simple",
                        weight = article.Poid,
                        extension_attributes = new
                        {
                            website_ids = website_ids,


                            stock_item = new
                            {
                                qty = stock,
                                min_qty = 1,
                                is_in_stock = is_in_stock
                            }
                        },
                        custom_attributes = CustomAttributes

                    },
                    saveOptions = true

                };
                return product;
            }
            else
            {*/

            var product = new
            {
                product = new
                {
                    //name = name,
                    sku = sku,
                    price = price,
                    //status = status,
                    //attribute_set_id = int.Parse(UtilsConfig.Attribute_set_id.ToString()),
                    //visibility = visibility,
                    type_id = "simple",
                    //weight = article.Poid,
                    extension_attributes = new
                    {
                        website_ids = website_ids,


                        stock_item = new
                        {
                            qty = stock,
                            min_qty = 1,
                            is_in_stock = true
                        }
                    }/*,
                    custom_attributes = CustomAttributes*/

                },
                saveOptions = true

            };
            return product;
            //}


        }
        public object SimpleProductjson(Article article, Gamme gamme = null, string value_index = null, string value_index2 = null, ProductSearchCriteria productMagento=null)
        {
            //is_in_stock = false;
            
            CustomAttribute custom_attribute = new CustomAttribute();
            CustomAttribute custom_attribute1 = new CustomAttribute();
            CustomAttributes = new List<CustomAttribute>();
            if (!String.IsNullOrEmpty(article.ArticleSubstitution))
            {
                CustomAttribute infolibre1 = new CustomAttribute();
                infolibre1.AttributeCode = "SBSITM";
                infolibre1.Value = article.ArticleSubstitution;
                CustomAttributes.Add(infolibre1);
            }
            if (!String.IsNullOrEmpty(article.Art_Stat_Gamme))
            {
                CustomAttribute infolibre2 = new CustomAttribute();
                infolibre2.AttributeCode = "SEAKEY";
                infolibre2.Value = article.Art_Stat_Gamme;
                CustomAttributes.Add(infolibre2);
            }

            foreach(InfoLibre info in article.infoLibre)
            {
                File.AppendAllText("Log\\infolibre.txt", info.Libelle.ToString() + Environment.NewLine);
                if (info.Libelle.Equals("Référence Commune") && !String.IsNullOrEmpty(info.Value))
                {
                    CustomAttribute infolibre3 = new CustomAttribute();
                    infolibre3.AttributeCode = "REFCOMMUNE";
                    infolibre3.Value = info.Value;
                    CustomAttributes.Add(infolibre3);
                }
            }

            if (!String.IsNullOrEmpty(article.UniteVente))
            {
                CustomAttribute infolibre4 = new CustomAttribute();
                infolibre4.AttributeCode = "SAU";
                infolibre4.Value = article.UniteVente;
                CustomAttributes.Add(infolibre4);
            }

            CustomAttribute infolibre5 = new CustomAttribute();
            infolibre5.AttributeCode = "mp_callforprice";
            infolibre5.Value = "-1";
            CustomAttributes.Add(infolibre5);

            System.IO.File.AppendAllText("Log\\product.txt", DateTime.Now + " recherche famille article : " + article.Famille + Environment.NewLine);
            CategoriesSearch categoriesSearch = CategoriesSearch.FromJson(UtilsWebservices.GetMagentoData("rest/V1/categories/list?searchCriteria[filter_groups][1][filters][0][field]=name&searchCriteria[filter_groups][1][filters][0][value]=" + article.Famille + "&searchCriteria[filter_groups][1][filters][0][condition_type]=eq"));
            //ProductCategories test = ProductCategories.FromJson(UtilsWebservices.GetMagentoData("rest/V1/categories/list?searchCriteria[filter_groups][1][filters][0][field]=name&searchCriteria[filter_groups][1][filters][0][value]="+article.Famille+"&searchCriteria[filter_groups][1][filters][0][condition_type]=eq"));
            if (categoriesSearch.TotalCount >0)
            {
                List<int> cats = new List<int>();
                cats.Add(int.Parse(categoriesSearch.Items[0].Id.ToString()));
                CustomAttribute cat = new CustomAttribute();
                cat.AttributeCode = "category_ids";
                cat.Value = cats;
                CustomAttributes.Add(cat);
            }
            else
            {
                List<int> cats = new List<int>();
                cats.Add(int.Parse(UtilsConfig.Category.ToString()));
                CustomAttribute cat = new CustomAttribute();
                cat.AttributeCode = "category_ids";
                cat.Value = cats;
                CustomAttributes.Add(cat);
            }
            if (!String.IsNullOrEmpty(article.CodeBarres))
            {
                CustomAttribute CodeEAN = new CustomAttribute();
                CodeEAN.AttributeCode = "code_ean";
                CodeEAN.Value = article.CodeBarres;
                CustomAttributes.Add(CodeEAN);
            }
            website_ids = new List<int>();
            website_ids.Add(int.Parse(UtilsConfig.Store.ToString()));
            if (gamme != null)
            {
                custom_attribute.AttributeCode = gamme.Intitule;
                custom_attribute.Value = value_index;
                CustomAttributes.Add(custom_attribute);
                if (gamme.Reference != null)
                {
                    sku = gamme.Reference;
                    if (article.IsDoubleGamme)
                    {
                        custom_attribute1.AttributeCode = gamme.Intitule2;
                        custom_attribute1.Value = value_index2;
                        CustomAttributes.Add(custom_attribute1);
                    }
                }
                else
                {
                    if (article.IsDoubleGamme)
                    {
                        sku = gamme.Value_Intitule + gamme.Value_Intitule2;
                        custom_attribute1.AttributeCode = gamme.Intitule2;
                        custom_attribute1.Value = value_index2;
                        CustomAttributes.Add(custom_attribute1);
                    }
                    else
                    {
                        sku = gamme.Value_Intitule;
                    }
                }
                
                price = gamme.Price;
                stock = gamme.Stock;
                if (article.IsDoubleGamme)
                {
                    name = article.Designation + " " + gamme.Value_Intitule + " " + gamme.Value_Intitule2;
                }
                else
                {
                    name = article.Designation + " " + gamme.Value_Intitule;
                }
                if (gamme.Sommeil)
                {
                    status = 2;
                }
                else
                {
                    if (gamme.Stock > 0)
                    {
                        status = 1;
                        is_in_stock = true;
                    }
                }

            }
            else
            {
                if (article.Sommeil)
                {
                    status = 2;
                }
                else
                {
                    status = 1;
                }
                if (article.Stock > 0)
                {
                    is_in_stock = true;
                }
                else
                {
                    is_in_stock = false;
                }
                sku = article.Reference;
                /*if (article.prixCatTarifs.Count > 0)
                {
                    if (article.prixCatTarifs[0].Price > 0)
                    {
                        price = article.prixCatTarifs[0].Price;
                    }
                }
                if (!(price >0))
                {
                    price = article.PrixVente;
                }*/
                price = article.PrixVente;
                stock = article.Stock;
                name = article.Designation;
            }
            File.AppendAllText("Log\\data.txt", price.ToString() + Environment.NewLine);

            /*if (!String.IsNullOrEmpty(article.Ecotaxe))
            {

                var value = new
                {
                    website_id = int.Parse(UtilsConfig.Store.ToString()),
                    country = "FR",
                    state = 0,
                    value = Double.Parse(article.Ecotaxe),
                    website_value = Double.Parse(article.Ecotaxe)
                };
                CustomAttribute ecotax = new CustomAttribute();
                ecotax.AttributeCode = "fpt_tax";
                ecotax.Value = value.ToString();
                CustomAttributes.Add(ecotax);
            }*/
            /*if (productMagento.TotalCount > 0)
            {
                status = productMagento.Items[0].Status;
                visibility = productMagento.Items[0].Visibility;
            }
            else
            {
                status = 2;
                visibility = 1;
            }
            */
            if (productMagento.TotalCount > 0)
            {
                status = productMagento.Items[0].Status;
                visibility = productMagento.Items[0].Visibility;
            }
            else
            {
                status = 2;
                visibility = 1;
            }

            /*if (productMagento.TotalCount >0)
            {
                var product = new
                {
                    product = new
                    {
                        sku = sku,
                        price = price,
                        attribute_set_id = int.Parse(UtilsConfig.Attribute_set_id.ToString()),
                        type_id = "simple",
                        weight = article.Poid,
                        extension_attributes = new
                        {
                            website_ids = website_ids,


                            stock_item = new
                            {
                                qty = stock,
                                min_qty = 1,
                                is_in_stock = is_in_stock
                            }
                        },
                        custom_attributes = CustomAttributes

                    },
                    saveOptions = true

                };
                return product;
            }
            else
            {*/
            visibility = 4;
            status = 1;
            var product = new
                {
                    product = new
                    {
                        name = name,
                        sku = sku,
                        price = price,
                        status = status,
                        attribute_set_id = int.Parse(UtilsConfig.Attribute_set_id.ToString()),
                        visibility = visibility,
                        type_id = "simple",
                        weight = article.Poid,
                        extension_attributes = new
                        {
                            website_ids = website_ids,


                            stock_item = new
                            {
                                qty = stock,
                                min_qty = 1,
                                is_in_stock = true
                            }
                        },
                        custom_attributes = CustomAttributes

                    },
                    saveOptions = true

                };
                return product;
            //}
            
            
        }
        public object BundleProductjson(Article article, Gamme gamme = null, string value_index = null, string value_index2 = null, ProductSearchCriteria productMagento=null)
        {
            is_in_stock = false;

            CustomAttribute custom_attribute = new CustomAttribute();
            CustomAttribute custom_attribute1 = new CustomAttribute();
            CustomAttributes = new List<CustomAttribute>();

            website_ids = new List<int>();
            website_ids.Add(int.Parse(UtilsConfig.Store.ToString()));
                if (article.Sommeil)
                {
                    status = 2;
                }
                else
                {
                    status = 1;
                }
                if (article.Stock > 0)
                {
                    is_in_stock = true;
                }
                else
                {
                    is_in_stock = false;
                }
                sku = article.Reference;
                price = article.PrixVente;
                stock = article.Stock;
                name = article.Designation;
            CustomAttribute sku_type = new CustomAttribute();
            sku_type.AttributeCode = "sku_type";
            sku_type.Value = "1";
            CustomAttributes.Add(sku_type);

            CustomAttribute price_type = new CustomAttribute();
            price_type.AttributeCode = "price_type";
            price_type.Value = "0";
            CustomAttributes.Add(price_type);

            CustomAttribute price_view = new CustomAttribute();
            sku_type.AttributeCode = "price_view";
            sku_type.Value = "0";
            CustomAttributes.Add(price_view);

            if (!String.IsNullOrEmpty(article.Ecotaxe))
            {

                var value = new
                {
                    website_id = int.Parse(UtilsConfig.Store.ToString()),
                    country = "FR",
                    state = 0,
                    value = Double.Parse(article.Ecotaxe),
                    website_value = Double.Parse(article.Ecotaxe)
                };
                CustomAttribute ecotax = new CustomAttribute();
                ecotax.AttributeCode = "fpt_tax";
                ecotax.Value = value.ToString();
                CustomAttributes.Add(ecotax);
            }
            if (productMagento.TotalCount > 0)
            {
                status = 2;
            }
            var product = new
            {
                product = new
                {
                    name = name,
                    sku = sku,
                    price = price,
                    status = status,
                    attribute_set_id = int.Parse(UtilsConfig.Attribute_set_id.ToString()),
                    visibility = 4,
                    type_id = "bundle",
                    weight = article.Poid,
                    extension_attributes = new
                    {
                        website_ids = website_ids,


                        stock_item = new
                        {
                            qty = 0,
                            //min_qty get value from infolibre
                            min_qty = 1,
                            is_in_stock = true
                        }
                    },
                    custom_attributes = CustomAttributes
                    /*new
                    {
                        attribute_code = custom_attribute.AttributeCode.ToString(),
                        value = custom_attribute.Value.ToString()
                    }*/
                },
                saveOptions = true

            };
            return product;
        }

        public object ConfigurableProductPriceCronjson(Article article, ProductSearchCriteria productMagento = null)
        {

            website_ids = new List<int>();
            website_ids.Add(int.Parse(UtilsConfig.Store.ToString()));


            if (productMagento.TotalCount > 0)
            {
                status = productMagento.Items[0].Status;
                visibility = productMagento.Items[0].Visibility;
            }
            else
            {
                status = 2;
                visibility = 1;
            }
            if (article.Stock > 0)
            {
                is_in_stock = true;
            }
            else
            {
                is_in_stock = false;
            }
            stock = article.Stock;
            var product = new
            {
                product = new
                {
                    //name = article.Designation,
                    sku = article.Reference,
                    price = 0,
                    //status = status,
                    //attribute_set_id = int.Parse(UtilsConfig.Attribute_set_id.ToString()),
                    //visibility = visibility,
                    type_id = "configurable"
                    //weight = article.Poid,
                    
                },
                saveOptions = true

            };
            return product;
        }


        public object ConfigurableProductCronjson(Article article, ProductSearchCriteria productMagento = null)
        {

            website_ids = new List<int>();
            website_ids.Add(int.Parse(UtilsConfig.Store.ToString()));


            if (productMagento.TotalCount > 0)
            {
                status = productMagento.Items[0].Status;
                visibility = productMagento.Items[0].Visibility;
            }
            else
            {
                status = 2;
                visibility = 1;
            }
            if (article.Stock > 0)
            {
                is_in_stock = true;
            }
            else
            {
                is_in_stock = false;
            }
            stock = article.Stock;
            var product = new
            {
                product = new
                {
                    //name = article.Designation,
                    sku = article.Reference,
                    price = 0,
                    //status = status,
                    //attribute_set_id = int.Parse(UtilsConfig.Attribute_set_id.ToString()),
                    //visibility = visibility,
                    type_id = "configurable",
                    //weight = article.Poid,
                    extension_attributes = new
                    {
                        website_ids = website_ids,

                        stock_item = new
                        {
                            qty = stock,
                            //min_qty get value from infolibre
                            //min_qty = 1,
                            is_in_stock = true
                        }
                    }
                },
                saveOptions = true

            };
            return product;
        }

        public object ConfigurableProductjson(Article article, ProductSearchCriteria productMagento = null)
        {

            website_ids = new List<int>();
            website_ids.Add(int.Parse(UtilsConfig.Store.ToString()));
            
            
            if (productMagento.TotalCount > 0)
            {
                status = productMagento.Items[0].Status;
                visibility = productMagento.Items[0].Visibility;
            }
            else
            {
                status = 2;
                visibility = 1;
            }
            if (article.Stock > 0)
            {
                is_in_stock = true;
            }
            else
            {
                is_in_stock = false;
            }
            stock = article.Stock;
            var product = new
            {
                product = new
                {
                    name = article.Designation,
                    sku = article.Reference,
                    price = 0,
                    status = status,
                    attribute_set_id = int.Parse(UtilsConfig.Attribute_set_id.ToString()),
                    visibility = visibility,
                    type_id = "configurable",
                    weight = article.Poid,
                    extension_attributes = new
                    {
                        website_ids = website_ids,
                        
                        stock_item = new
                        {
                            qty = stock,
                            //min_qty get value from infolibre
                            //min_qty = 1,
                            is_in_stock = true
                        }
                    }
                },
                saveOptions = true

            };
            return product;
        }
        public object CustomProductStock(Article article,Gamme gamme=null, ProductSearchCriteria productMagento = null)
        {
            if (gamme != null)
            {
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
                stock = 0;
                if (article.IsDoubleGamme)
                {
                    name = article.Designation + " " + gamme.Value_Intitule + " " + gamme.Value_Intitule2;
                }
                else
                {
                    name = article.Designation + " " + gamme.Value_Intitule;
                }
                if (gamme.Sommeil)
                {
                    status = 2;
                }
                else
                {
                    if (gamme.Stock > 0)
                    {
                        stock = gamme.Stock;
                        status = 1;
                        is_in_stock = true;
                    }
                }

            }
            if (productMagento.TotalCount > 0)
            {
                status = productMagento.Items[0].Status;
                visibility = productMagento.Items[0].Visibility;
            }
            else
            {
                status = 2;
                visibility = 1;
            }


            var CustomProductStock = new
            {
                product = new
                {
                    name = name,
                    sku = sku,
                    status = status,
                    attribute_set_id = int.Parse(UtilsConfig.Attribute_set_id.ToString()),
                    visibility = visibility,
                    type_id = "simple",
                    weight = article.Poid,
                    extension_attributes = new
                    {
                        website_ids = website_ids,

                        stock_item = new
                        {
                            qty = stock,
                            is_in_stock = true
                        }
                    }
                },
                saveOptions = true
            };
            return CustomProductStock;
        }
        public object CustomProductPrice(Article article, Gamme gamme = null, ProductSearchCriteria productMagento = null)
        {
            if (gamme != null)
            {
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
                price = gamme.Price;
                //stock = gamme.Stock;
                if (article.IsDoubleGamme)
                {
                    name = article.Designation + " " + gamme.Value_Intitule + " " + gamme.Value_Intitule2;
                }
                else
                {
                    name = article.Designation + " " + gamme.Value_Intitule;
                }
                if (gamme.Sommeil)
                {
                    status = 2;
                }
                else
                {
                    if (gamme.Stock > 0)
                    {
                        status = 1;
                        is_in_stock = true;
                    }
                }

            }
            if (productMagento.TotalCount > 0)
            {
                status = productMagento.Items[0].Status;
                visibility = productMagento.Items[0].Visibility;
            }
            else
            {
                status = 2;
                visibility = 1;
            }
            var CustomProductPrice = new
            {
                product = new
                {
                    name = name,
                    sku = sku,
                    status = status,
                    price = price,
                    attribute_set_id = int.Parse(UtilsConfig.Attribute_set_id.ToString()),
                    visibility = 4,
                    type_id = "simple",
                    weight = article.Poid,
                    extension_attributes = new
                    {
                        website_ids = website_ids
                    }
                },
                saveOptions = true
            };
            return CustomProductPrice;
        }
    }
}
