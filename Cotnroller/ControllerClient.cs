using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Objets100cLib;
using WebservicesSage.Object;
using WebservicesSage.Singleton;
using WebservicesSage.Utils;
using WebservicesSage.Utils.Enums;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Xml;
using WebservicesSage.Object.CustomerSearch;
using WebservicesSage.Object.Order;
using System.Globalization;
using System.Timers;
using WebservicesSage.Object.CustomerSearchByEmail;
using System.Data.SqlClient;

namespace WebservicesSage.Cotnroller
{
    public static class ControllerClient
    {

        
        /// <summary>
        /// Permets de remonter toute la base clients de SAGE vers Prestashop
        /// Ne remonte que les clients avec un mail 
        /// </summary>
        public static void SendAllClients()
        {
            try
            {
                var compta = SingletonConnection.Instance.Gescom.CptaApplication;
                var clientsSageObj = compta.FactoryClient.List;

                //var clients = GetListOfClientToProcess(clientsSageObj);
                foreach (IBOClient3 clientSageObj in clientsSageObj)
                {
                    Client client = new Client(clientSageObj);
                    /*string clientXML = UtilsSerialize.SerializeObject<Client>(client);
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(clientXML);*/
                    CustomerSearchByEmail ClientSearch = CustomerSearchByEmail.FromJson(UtilsWebservices.GetMagentoData("rest/V1/customers/search" + UtilsWebservices.SearchOrderCriteria("email", client.Email, "eq")));
                    Customer customerMagento = new Customer();

                    if (ClientSearch.Items.Count > 0)
                    {
                        CustomerSearch clientM = UtilsWebservices.GetClientCtNum(ClientSearch.Items[0].Id.ToString());
                        var jsonClient = JsonConvert.SerializeObject(customerMagento.UpdateCustomer(client.CT_NUM, "", clientM));
                        UtilsWebservices.SendDataJson(jsonClient, @"rest/all/V1/customers/" + ClientSearch.Items[0].Id.ToString(), "PUT");
                    }
                    else
                    {
                        var jsonClient = JsonConvert.SerializeObject(customerMagento.NewCustomer(client, clientSageObj, ClientSearch));
                        UtilsWebservices.SendDataJson(jsonClient, @"rest/all/V1/customers/");
                    }
                }

                MessageBox.Show("end client sync", "ok",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Information);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error",
                   MessageBoxButtons.OK,
                   MessageBoxIcon.Information);
            }

        }

        public static void SendClient(string ct_num)
        {
            try
            {
                var compta = SingletonConnection.Instance.Gescom.CptaApplication;
                var clientsSageObj = compta.FactoryClient.ReadNumero(ct_num);
                var clients = GetListOfClientToProcess(clientsSageObj);
                Client client = new Client(clientsSageObj);
                if (!String.IsNullOrEmpty(client.Email))
                {
                    /*string clientXML = UtilsSerialize.SerializeObject<Client>(client);
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(clientXML);*/
                    CustomerSearchByEmail ClientSearch = CustomerSearchByEmail.FromJson(UtilsWebservices.GetMagentoData("rest/V1/customers/search" + UtilsWebservices.SearchOrderCriteria("email", client.Email, "eq")));
                    Customer customerMagento = new Customer();

                    if (ClientSearch.Items.Count > 0)
                    {
                        CustomerSearch clientM = UtilsWebservices.GetClientCtNum(ClientSearch.Items[0].Id.ToString());
                        var jsonClient = JsonConvert.SerializeObject(customerMagento.UpdateCustomer(ct_num, "", clientM));
                        UtilsWebservices.SendDataJson(jsonClient, @"rest/all/V1/customers/" + ClientSearch.Items[0].Id.ToString(), "PUT");
                    }
                    else
                    {
                        var jsonClient = JsonConvert.SerializeObject(customerMagento.NewCustomer(client, clientsSageObj, ClientSearch));
                        UtilsWebservices.SendDataJson(jsonClient, @"rest/all/V1/customers/");
                    }
                    MessageBox.Show("end client sync", "ok",
                                   MessageBoxButtons.OK,
                                   MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Client n'a pas d'adresse mail", "Error",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Information);
                }
                
                
            }
            catch (Exception e)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now + ct_num + Environment.NewLine);
                sb.Append(DateTime.Now + e.Message + Environment.NewLine);
                sb.Append(DateTime.Now + e.StackTrace + Environment.NewLine);
                File.AppendAllText("Log\\SyncAll.txt", sb.ToString());
                sb.Clear();
                MessageBox.Show(e.Message, "Error",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Permet de vérifier si un client comporte des erreur ou non
        /// </summary>
        /// <param name="client">Client à tester</param>
        /// <returns></returns>
        private static bool HandleClientError(Client client)
        {
            bool error = false;

            if (String.IsNullOrEmpty(client.Email))
            {
                //error = true;
                // SingletonUI.Instance.LogBox.Invoke((MethodInvoker)(() => SingletonUI.Instance.LogBox.AppendText("Client :  " + client.Intitule + " No Mail Found" + Environment.NewLine)));


                // on affiche une erreur + log 
            }

            return error;
        }

        /// <summary>
        /// Permet de récupérer une liste de Client depuis une liste de Client SAGE
        /// </summary>
        /// <param name="clientsSageObj">List de client SAGE</param>
        /// <returns></returns>
        private static List<Client> GetListOfClientToProcess(IBOClient3 clientSageObj)
        {
            List<Client> clientToProcess = new List<Client>();
                    if (!String.IsNullOrEmpty(clientSageObj.Telecom.EMail))
                    {
                        Client client = new Client(clientSageObj);
                        //client.setClientLivraisonAdresse();
                        clientToProcess.Add(client);
                    }
                    else
                    {
                    // On ajoute les contacts à la liste
                        
                    }
            return clientToProcess;
        }

        private static List<Client> GetClientToProcess(IBOClient3 clientsSageObj)
        {
            List<Client> clientToProcess = new List<Client>();

            Client client = new Client(clientsSageObj);

            if (!HandleClientError(client))
            {
                //client.setClientLivraisonAdresse();
                //clientToProcess.Add(client);
            }
            return clientToProcess;
        }

        /// <summary>
        /// Permet de vérifier si un Client existe dans SAGE
        /// </summary>
        /// <param name="CT_num"></param>
        /// <returns></returns>
        public static bool CheckIfClientExist(string CT_num)
        {
            if (String.IsNullOrEmpty(CT_num))
            {
                return false;
            }
            else
            {
                var compta = SingletonConnection.Instance.Gescom.CptaApplication;
                if (compta.FactoryTiers.ExistNumero(CT_num))// FactoryClient.ExistNumero(CT_num))
                {
                    //File.AppendAllText("Log\\GetOrderCustomer.txt", "Client existe : " + CT_num + Environment.NewLine);
                    return true;

                }
                else
                {
                    //File.AppendAllText("Log\\GetOrderCustomer.txt", "Client n'existe pas : " + CT_num + Environment.NewLine);
                    return false;
                }

            }

        }

        /// <summary>
        /// ToDo
        /// </summary>
        public static void CreateNewClient()
        {

        }

        /// <summary>
        /// Permet de crée un Client dans la base SAGE depuis un objet json de prestashop
        /// </summary>
        /// <param name="jsonClient">json du Client à crée</param>
        /// <returns></returns>
        public static string CreateNewClient(CustomerSearch customer, OrderSearchItem order)
        {
            //JObject customer = JObject.Parse(jsonClient);

            var compta = SingletonConnection.Instance.Gescom.CptaApplication;
            var gescom = SingletonConnection.Instance.Gescom;
            IBOClient3 clientSage = (IBOClient3)compta.FactoryClient.Create();
            clientSage.SetDefault();
            //File.AppendAllText("Log\\GetCustomer.txt", tarifsSearch.ToString() + Environment.NewLine);

            /*
            var test = clientSage.FactoryTiersContact.Create();
            test.Factory.List[0].*/
            Object.CustomerSearch.Address defaultAddress = new Object.CustomerSearch.Address();
            foreach (Object.CustomerSearch.Address addressCustomer in customer.Addresses)
            {
                //if (addressCustomer.Id == customer.DefaultBilling)
                //{
                    defaultAddress = addressCustomer;
                   // break;
                //}
            }
            try
            {
                
                if (String.IsNullOrEmpty(UtilsConfig.PrefixClient))
                {
                    // pas de configuration renseigner pour le prefix client
                    // todo log
                    //int iterID = Int32.Parse(UtilsWebservices.SendDataNoParse(UtilsConfig.BaseUrl + EnumEndPoint.Client.Value, "getClientIterationSage&clientID=" + customer["id"].ToString()));
                    int iterID = Int32.Parse(customer.Id.ToString());
                    while (compta.FactoryClient.ExistNumero(iterID.ToString()))
                    {
                        iterID++;
                    }
                    clientSage.CT_Num = iterID.ToString();
                    File.AppendAllText("Log\\CreateClient.txt", Environment.NewLine + "CT_Num: " + iterID.ToString());

                }
                else
                {
                    clientSage.CT_Num = UtilsConfig.PrefixClient + customer.Id.ToString();
                    File.AppendAllText("Log\\CreateClient.txt", Environment.NewLine + "CT_Num: " + UtilsConfig.PrefixClient + customer.Id.ToString());


                }
                string intitule;//TODO TAKE CARE OF NULL VALUE
                if (!String.IsNullOrEmpty(defaultAddress.company))
                {
                    intitule = defaultAddress.company.ToUpper();
                    File.AppendAllText("Log\\CreateClient.txt", Environment.NewLine+"company: "+ defaultAddress.company);
                }
                else
                {
                    intitule = defaultAddress.Firstname.ToString().ToUpper() + " " + defaultAddress.Lastname.ToString().ToUpper();
                    File.AppendAllText("Log\\CreateClient.txt", Environment.NewLine + "Firstname: " + defaultAddress.Firstname);

                }
                if (intitule.Length > 35)
                {
                    clientSage.CT_Intitule = intitule.Substring(0, 35);
                }
                else
                {
                    clientSage.CT_Intitule = intitule;
                }

                /*IBOCompteG3 compteG = compta.FactoryCompteG.ReadNumero("41110000");
                clientSage.CompteGPrinc = compteG;*/
                if (defaultAddress.Street.Count > 0)
                {
                    if (defaultAddress.Street[0].Length > 35)
                    {
                        clientSage.Adresse.Adresse = defaultAddress.Street[0].ToString().Substring(0, 35);
                        File.AppendAllText("Log\\CreateClient.txt", Environment.NewLine + "Street: " + defaultAddress.Street[0]);

                    }
                    else
                    {
                        clientSage.Adresse.Adresse = defaultAddress.Street[0].ToString();
                        File.AppendAllText("Log\\CreateClient.txt", Environment.NewLine + "Street: " + defaultAddress.Street[0]);

                    }
                }
                if (defaultAddress.Street.Count > 1)
                {
                    if (defaultAddress.Street[1].Length > 35)
                    {
                        clientSage.Adresse.Complement = defaultAddress.Street[1].ToString().Substring(0, 35);
                        File.AppendAllText("Log\\CreateClient.txt", Environment.NewLine + "Complement: " + defaultAddress.Street[1]);

                    }
                    else
                    {
                        clientSage.Adresse.Complement = defaultAddress.Street[1].ToString();
                        File.AppendAllText("Log\\CreateClient.txt", Environment.NewLine + "Complement: " + defaultAddress.Street[1]);

                    }

                }

                clientSage.Adresse.CodePostal = defaultAddress.Postcode.ToString();
                File.AppendAllText("Log\\CreateClient.txt", Environment.NewLine + "CodePostal: " + defaultAddress.Postcode);

                clientSage.Adresse.Ville = defaultAddress.City.ToString();
                File.AppendAllText("Log\\CreateClient.txt", Environment.NewLine + "City: " + defaultAddress.City);

                var region = CultureInfo
                                .GetCultures(CultureTypes.SpecificCultures)
                                .Select(ci => new RegionInfo(ci.LCID))
                                .FirstOrDefault(rg => rg.TwoLetterISORegionName == defaultAddress.CountryId);
                clientSage.Adresse.Pays = region.DisplayName.ToString();
                File.AppendAllText("Log\\CreateClient.txt", Environment.NewLine + "Pays: " + region.DisplayName.ToString());

                clientSage.Telecom.Telephone = defaultAddress.Telephone.ToString();
                File.AppendAllText("Log\\CreateClient.txt", Environment.NewLine + "Telephone: " + defaultAddress.Telephone.ToString().ToString());

                //clientSage.Telecom.Telecopie = customer.Addresses[0]..ToString();




                clientSage.Write();

                /*
                if (String.IsNullOrEmpty(UtilsConfig.CatTarif))
                {
                    // pas de configuration renseigner pour la cat tarif par defaut
                    // todo log
                }
                else
                {
                    clientSage.CatTarif = gescom.FactoryCategorieTarif.ReadIntitule(UtilsConfig.CatTarif);
                }
                if (String.IsNullOrEmpty(UtilsConfig.CompteG))
                {
                    // pas de configuration renseigner pour la cat tarif par defaut
                    // todo log
                }
                else
                {
                    clientSage.CompteGPrinc = compta.FactoryCompteG.ReadNumero(UtilsConfig.CompteGnum);
                }
                */
                string contactS = defaultAddress.Firstname.ToString() + " " + defaultAddress.Lastname.ToString();
                if (contactS.Length > 35)
                {
                    clientSage.CT_Contact = contactS.Substring(0, 35);
                }
                else
                {
                    clientSage.CT_Contact = contactS;
                }

                clientSage.Telecom.EMail = customer.Email.ToString();


                // abrégé client 
                if (intitule.Length > 17)
                {
                    clientSage.CT_Classement = intitule.Substring(0, 17);
                }
                else
                {
                    clientSage.CT_Classement = intitule;
                }
                if (!String.IsNullOrEmpty(customer.taxvat))
                {
                    clientSage.CT_Identifiant = customer.taxvat;
                }
                else
                {
                    clientSage.CT_Identifiant = "";
                }
                //clientSage.Write();

                

                /* if (region.DisplayName.ToString().ToUpper() != "FRANCE" && !clientSage.CT_Identifiant.ToString().Equals(""))
                 {
                     try
                     {
                         IBICategorieCompta categorieCompta = gescom.FactoryCategorieComptaVente.ReadIntitule(UtilsConfig.CategorieComptableForeigner);
                         clientSage.CategorieCompta = categorieCompta;
                     }
                     catch (Exception e)
                     {
                         UtilsMail.SendErrorMail(DateTime.Now + e.Message + Environment.NewLine + e.StackTrace + Environment.NewLine, "CREATE CLIENT CATEGORIE COMPTABLE");
                     }
                 }*/
                clientSage.Write();
            }
            catch (Exception e)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now + "Erreur création client : " + Environment.NewLine);
                sb.Append(DateTime.Now + e.Message + Environment.NewLine);
                sb.Append(DateTime.Now + e.StackTrace + Environment.NewLine);
                File.AppendAllText("Log\\CreateClient.txt", sb.ToString());
                
            }

            //clientSage.Write();

            try
            {
                IBOClientLivraison3 addrprinc = (IBOClientLivraison3)clientSage.FactoryClientLivraison.Create();

                if (!String.IsNullOrEmpty(order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.company))
                {
                    if (order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.company.ToString().Length > 35)
                    {
                        addrprinc.LI_Intitule = order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.company.ToUpper().Substring(0, 35);
                    }
                    else
                    {
                        addrprinc.LI_Intitule = order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.company.ToUpper();
                    }
                }
                else
                {
                    string intitule = "";
                    intitule = order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Firstname + " " + order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Lastname;
                    if (intitule.Length > 35)
                    {
                        intitule = intitule.Substring(0, 35);
                    }
                    addrprinc.LI_Intitule = intitule.ToUpper();
                }

                if (order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Street[0].ToString().Length > 35)
                {
                    addrprinc.Adresse.Adresse = order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Street[0].ToString().Substring(0, 35);
                }
                else
                {
                    addrprinc.Adresse.Adresse = order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Street[0].ToString();
                }
                if (order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Street.Count > 1)
                {
                    addrprinc.Adresse.Complement = order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Street[1].ToString();
                }
                addrprinc.Adresse.CodePostal = order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Postcode;
                addrprinc.Adresse.Ville = order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.City;
                var region = CultureInfo
                                    .GetCultures(CultureTypes.SpecificCultures)
                                    .Select(ci => new RegionInfo(ci.LCID))
                                    .FirstOrDefault(rg => rg.TwoLetterISORegionName == order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.CountryId);
                addrprinc.Adresse.Pays = region.DisplayName.ToString();
                if (!String.IsNullOrEmpty(order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Telephone.ToString()))
                {
                    addrprinc.Telecom.Telephone = order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Telephone.ToString();
                }
                
                //addrprinc.Telecom.Telecopie = order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Telecopie.ToString();

                if (String.IsNullOrEmpty(UtilsConfig.CondLivraison))
                {
                    // pas de configuration renseigner pour CondLivraison par defaut
                    // todo log
                }
                else
                {
                    addrprinc.ConditionLivraison = gescom.FactoryConditionLivraison.ReadIntitule(UtilsConfig.CondLivraison);
                }
                if (String.IsNullOrEmpty(UtilsConfig.Expedition))
                {
                    // pas de configuration renseigner pour Expedition par defaut
                    // todo log
                }
                else
                {
                    addrprinc.Expedition = gescom.FactoryExpedition.ReadIntitule(UtilsConfig.Expedition);
                }
                clientSage.LivraisonPrincipal = addrprinc;
                addrprinc.Write();
            }
            catch (Exception e)
            {
                UtilsMail.SendErrorMail(DateTime.Now + e.Message + Environment.NewLine + e.StackTrace + Environment.NewLine, "CREATE CLIENT ADRESS P");
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now + e.Message + Environment.NewLine);
                sb.Append(DateTime.Now + e.StackTrace + Environment.NewLine);
                File.AppendAllText("Log\\CreateClient.txt", sb.ToString());
                sb.Clear();
                return "";
            }

            try
            {

                foreach (IBOCollaborateur representant in compta.FactoryCollaborateur.List)
                {
                    if (representant.Nom.ToString().Equals("MATERIEL DE PRO COM"))
                    {
                        clientSage.Representant = representant;
                        
                    }

                }

                foreach (IBPCategorieTarif cat in gescom.FactoryCategorieTarif.List)
                {
                    if (cat.CT_Intitule.ToString().ToUpper().Equals("Materieldepro.com".ToUpper()))
                    {
                        clientSage.CatTarif = cat;
                    }

                }

                if (!defaultAddress.CountryId.ToUpper().Equals("FR"))
                {
                    foreach (IBPCategorieComptaVente cat in gescom.FactoryCategorieComptaVente.List)
                    {
                        if (cat.Intitule.ToString().ToUpper().Equals("Vente export".ToUpper()))
                        {
                            clientSage.CategorieCompta = cat;
                        }

                    }
                }
                else
                {
                    foreach (IBPCategorieComptaVente cat in gescom.FactoryCategorieComptaVente.List)
                    {
                        if (cat.Intitule.ToString().ToUpper().Equals("Vente France".ToUpper()))
                        {
                            clientSage.CategorieCompta = cat;
                        }

                    }
                }

                clientSage.Write();

            }
            catch(Exception e)
            {
                UtilsMail.SendErrorMail(DateTime.Now + e.Message + Environment.NewLine + e.StackTrace + Environment.NewLine, "CREATE CLIENT ADRESS P");
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now + e.Message + Environment.NewLine);
                sb.Append(DateTime.Now + e.StackTrace + Environment.NewLine);
                File.AppendAllText("Log\\CreateClient.txt", sb.ToString());
                sb.Clear();
                return "";
            }

            /*try
            {
                
            }
            catch (Exception e)
            {
                UtilsMail.SendErrorMail(DateTime.Now + e.Message + Environment.NewLine + e.StackTrace + Environment.NewLine, "CREATE CLIENT ADRESS P");
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now + e.Message + Environment.NewLine);
                sb.Append(DateTime.Now + e.StackTrace + Environment.NewLine);
                File.AppendAllText("Log\\CreateClient.txt", sb.ToString());
                sb.Clear();
                return "";
            }*/



            return clientSage.CT_Num;
        }

        public static string CreateNewClientDevis(CustomerSearch customer, Object.Devis.Devis order)
        {
            //JObject customer = JObject.Parse(jsonClient);

            var compta = SingletonConnection.Instance.Gescom.CptaApplication;
            var gescom = SingletonConnection.Instance.Gescom;
            IBOClient3 clientSage = (IBOClient3)compta.FactoryClient.Create();
            clientSage.SetDefault();/*
            var test = clientSage.FactoryTiersContact.Create();
            test.Factory.List[0].*/
            try
            {
                if (customer.Addresses.Count >0)
                {
                    Object.CustomerSearch.Address defaultAddress = new Object.CustomerSearch.Address();
                    foreach (Object.CustomerSearch.Address addressCustomer in customer.Addresses)
                    {
                        if (addressCustomer.Id == customer.DefaultBilling)
                        {
                            defaultAddress = addressCustomer;
                            break;
                        }
                    }
                    if (defaultAddress.Street[0].Length > 35)
                    {
                        clientSage.Adresse.Adresse = defaultAddress.Street[0].ToString().Substring(0, 35);
                    }
                    else
                    {
                        clientSage.Adresse.Adresse = defaultAddress.Street[0].ToString();
                    }
                    if (defaultAddress.Street.Count > 1)
                    {
                        clientSage.Adresse.Complement = defaultAddress.Street[1].ToString();
                    }

                    clientSage.Adresse.CodePostal = defaultAddress.Postcode.ToString();
                    clientSage.Adresse.Ville = defaultAddress.City.ToString();
                    var region = CultureInfo
                                    .GetCultures(CultureTypes.SpecificCultures)
                                    .Select(ci => new RegionInfo(ci.LCID))
                                    .FirstOrDefault(rg => rg.TwoLetterISORegionName == defaultAddress.CountryId);
                    clientSage.Adresse.Pays = region.DisplayName.ToString();
                    clientSage.Telecom.Telephone = defaultAddress.Telephone.ToString();
                    string intitule;//TODO TAKE CARE OF NULL VALUE
                    if (!String.IsNullOrEmpty(defaultAddress.company))
                    {
                        intitule = defaultAddress.company.ToUpper();
                    }
                    else
                    {
                        intitule = defaultAddress.Firstname.ToString().ToUpper() + " " + defaultAddress.Lastname.ToString().ToUpper();
                    }
                    string contactS = defaultAddress.Firstname.ToString() + " " + defaultAddress.Lastname.ToString();
                    if (contactS.Length > 35)
                    {
                        clientSage.CT_Contact = contactS.Substring(0, 35);
                    }
                    else
                    {
                        clientSage.CT_Contact = contactS;
                    }

                    
                    if (intitule.Length > 35)
                    {
                        clientSage.CT_Intitule = intitule.Substring(0, 35);
                    }
                    else
                    {
                        clientSage.CT_Intitule = intitule;
                    }

                    // abrégé client 
                    if (intitule.Length > 17)
                    {
                        clientSage.CT_Classement = intitule.Substring(0, 17);
                    }
                    else
                    {
                        clientSage.CT_Classement = intitule;
                    }
                    /*
                    if (region.DisplayName.ToString().ToUpper() != "FRANCE" && !clientSage.CT_Identifiant.ToString().Equals(""))
                    {
                        try
                        {
                            IBICategorieCompta categorieCompta = gescom.FactoryCategorieComptaVente.ReadIntitule(UtilsConfig.CategorieComptableForeigner);
                            clientSage.CategorieCompta = categorieCompta;
                        }
                        catch (Exception e)
                        {
                            UtilsMail.SendErrorMail(DateTime.Now + e.Message + Environment.NewLine + e.StackTrace + Environment.NewLine, "CREATE CLIENT CATEGORIE COMPTABLE");
                        }
                    }*/
                }
                else
                {
                    string intitule;//TODO TAKE CARE OF NULL VALUE
                    intitule = customer.Firstname.ToString().ToUpper() + " " + customer.Lastname.ToString().ToUpper();
                    if (intitule.Length > 35)
                    {
                        clientSage.CT_Intitule = intitule.Substring(0, 35);
                    }
                    else
                    {
                        clientSage.CT_Intitule = intitule;
                    }
                }


                //clientSage.Telecom.Telecopie = customer.Addresses[0]..ToString();

                clientSage.Telecom.EMail = customer.Email.ToString();

                if (String.IsNullOrEmpty(UtilsConfig.PrefixClient))
                {
                    // pas de configuration renseigner pour le prefix client
                    // todo log
                    //int iterID = Int32.Parse(UtilsWebservices.SendDataNoParse(UtilsConfig.BaseUrl + EnumEndPoint.Client.Value, "getClientIterationSage&clientID=" + customer["id"].ToString()));
                    int iterID = Int32.Parse(customer.Id.ToString());
                    while (compta.FactoryClient.ExistNumero(iterID.ToString()))
                    {
                        iterID++;
                    }
                    clientSage.CT_Num = iterID.ToString();
                }
                else
                {
                    clientSage.CT_Num = UtilsConfig.PrefixClient + customer.Id.ToString();
                }
                clientSage.Write();
                foreach (Object.CustomerSearch.CustomAttribute customarttribute in customer.CustomAttributes)
                {
                    if (customarttribute.AttributeCode.Equals("customer_type"))
                    {
                        if (customarttribute.Value.ToString().Equals("1"))
                        {
                            clientSage.InfoLibre[3] = "PRIMAIRE/MATERNELLE";
                        }
                        if (customarttribute.Value.ToString().Equals("2"))
                        {
                            clientSage.InfoLibre[3] = "COLLEGE";
                        }
                        if (customarttribute.Value.ToString().Equals("3"))
                        {
                            clientSage.InfoLibre[3] = "LYCEE";
                        }
                        if (customarttribute.Value.ToString().Equals("4"))
                        {
                            clientSage.InfoLibre[3] = "POST-BAC";
                        }
                        if (customarttribute.Value.ToString().Equals("5"))
                        {
                            clientSage.InfoLibre[3] = "GROUPE SCOLAIRE";
                        }
                        if (customarttribute.Value.ToString().Equals("6"))
                        {
                            clientSage.InfoLibre[3] = "INSTITUT SPECIALISE";
                        }
                        if (customarttribute.Value.ToString().Equals("7"))
                        {
                            clientSage.InfoLibre[3] = "CENTRE DE FORMATION";
                        }
                        if (customarttribute.Value.ToString().Equals("8"))
                        {
                            clientSage.InfoLibre[3] = "SOCIETE";
                        }
                        if (customarttribute.Value.ToString().Equals("9"))
                        {
                            clientSage.InfoLibre[3] = "PARTICULIER";
                        }
                        if (customarttribute.Value.ToString().Equals("10"))
                        {
                            clientSage.InfoLibre[3] = "ASSOCIATION";
                        }
                        if (customarttribute.Value.ToString().Equals("11"))
                        {
                            clientSage.InfoLibre[3] = "ADMINISTRATION";
                        }
                        if (customarttribute.Value.ToString().Equals("12"))
                        {
                            clientSage.InfoLibre[3] = "CENTRALE D\'ACHATS";
                        }

                    }
                }
                //clientSage.InfoLibre[3] = "test";
                /*if (String.IsNullOrEmpty(UtilsConfig.CatTarif))
                {
                    // pas de configuration renseigner pour la cat tarif par defaut
                    // todo log
                }
                else
                {
                    clientSage.CatTarif = gescom.FactoryCategorieTarif.ReadIntitule(UtilsConfig.CatTarif);
                }
                if (String.IsNullOrEmpty(UtilsConfig.CompteG))
                {
                    // pas de configuration renseigner pour la cat tarif par defaut
                    // todo log
                }
                else
                {
                    clientSage.CompteGPrinc = compta.FactoryCompteG.ReadNumero(UtilsConfig.CompteGnum);
                }

                */
                if (!String.IsNullOrEmpty(customer.taxvat))
                {
                    clientSage.CT_Identifiant = customer.taxvat;
                }
                else
                {
                    clientSage.CT_Identifiant = "";
                }


                
            }
            catch (Exception e)
            {
                UtilsMail.SendErrorMail(DateTime.Now + e.Message + Environment.NewLine + e.StackTrace + Environment.NewLine, "CREATE CLIENT INFO G");
            }

            clientSage.Write();
/*
            try
            {
                IBOClientLivraison3 addrprinc = (IBOClientLivraison3)clientSage.FactoryClientLivraison.Create();

                if (!String.IsNullOrEmpty(order.Items[0]. ))//ExtensionAttributes.ShippingAssignments[0].Shipping.Address.company))
                {
                    addrprinc.LI_Intitule = order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.company.ToUpper();
                }
                else
                {
                    string intitule = "";
                    intitule = order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Firstname + " " + order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Lastname;
                    if (intitule.Length > 35)
                    {
                        intitule.Substring(0, 35);
                    }
                    addrprinc.LI_Intitule = intitule.ToUpper();
                }

                if (order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Street[0].ToString().Length > 35)
                {
                    addrprinc.Adresse.Adresse = order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Street[0].ToString().Substring(0, 35);
                }
                else
                {
                    addrprinc.Adresse.Adresse = order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Street[0].ToString();
                }
                if (order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Street.Count > 1)
                {
                    addrprinc.Adresse.Complement = order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Street[1].ToString();
                }
                addrprinc.Adresse.CodePostal = order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Postcode;
                addrprinc.Adresse.Ville = order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.City;
                var region = CultureInfo
                                    .GetCultures(CultureTypes.SpecificCultures)
                                    .Select(ci => new RegionInfo(ci.LCID))
                                    .FirstOrDefault(rg => rg.TwoLetterISORegionName == order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.CountryId);
                addrprinc.Adresse.Pays = region.DisplayName.ToString();
                addrprinc.Telecom.Telephone = order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Telephone.ToString();
                //addrprinc.Telecom.Telecopie = order.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Telecopie.ToString();

                if (String.IsNullOrEmpty(UtilsConfig.CondLivraison))
                {
                    // pas de configuration renseigner pour CondLivraison par defaut
                    // todo log
                }
                else
                {
                    addrprinc.ConditionLivraison = gescom.FactoryConditionLivraison.ReadIntitule(UtilsConfig.CondLivraison);
                }
                if (String.IsNullOrEmpty(UtilsConfig.Expedition))
                {
                    // pas de configuration renseigner pour Expedition par defaut
                    // todo log
                }
                else
                {
                    addrprinc.Expedition = gescom.FactoryExpedition.ReadIntitule(UtilsConfig.Expedition);
                }
                clientSage.LivraisonPrincipal = addrprinc;
                addrprinc.Write();
            }
            catch (Exception e)
            {
                UtilsMail.SendErrorMail(DateTime.Now + e.Message + Environment.NewLine + e.StackTrace + Environment.NewLine, "CREATE CLIENT ADRESS P");
            }


            // on envoie une notification à préstashop pour lui informer de la créeation dans SAGE du client
            //UtilsWebservices.SendDataNoParse(UtilsConfig.BaseUrl + EnumEndPoint.Client.Value, "updateCTnum&clientID=" + customer["id"].ToString() + "&ct_num=" + clientSage.CT_Num);
            //UtilsWebservices.SendDataNoParse(UtilsConfig.BaseUrl + EnumEndPoint.Client.Value, "updateIter&iter=" + clientSage.CT_Num);

            */
            return clientSage.CT_Num;
        }
        public static string CheckIfClientEmailExist(string email)
        {
            try
            {
                string ct_num = "";
                string sql1 = "SELECT CT_Num FROM [" + System.Configuration.ConfigurationManager.AppSettings["DBNAME"].ToString() + "].[dbo].[F_COMPTET] WHERE CT_EMail like '" + email + "'";
                //File.AppendAllText("Log\\SQL.txt", DateTime.Now + sql1.ToString() + Environment.NewLine);
                SqlDataReader CTnumEmailCompte = DB.Select(sql1);
                while (CTnumEmailCompte.Read())
                {
                    if (!String.IsNullOrEmpty(CTnumEmailCompte.GetValue(0).ToString()))
                    {

                        ct_num = CTnumEmailCompte.GetValue(0).ToString();
                    }
                }
                DB.Disconnect();
                string sql2 = "SELECT CT_Num FROM [" + System.Configuration.ConfigurationManager.AppSettings["DBNAME"].ToString() + "].[dbo].[F_CONTACTT] WHERE CT_EMail like '" + email + "'";
                //File.AppendAllText("Log\\SQL.txt", DateTime.Now + sql2.ToString() + Environment.NewLine);
                SqlDataReader CTnumEmailContact = DB.Select(sql2);
                while (CTnumEmailContact.Read())
                {
                    if (!String.IsNullOrEmpty(CTnumEmailContact.GetValue(0).ToString()))
                    {
                        ct_num = CTnumEmailContact.GetValue(0).ToString();
                    }
                }
                DB.Disconnect();
                return ct_num;
            }
            catch(Exception s)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now + s.Message + Environment.NewLine);
                sb.Append(DateTime.Now + s.StackTrace + Environment.NewLine);
                File.AppendAllText("Log\\order.txt", sb.ToString());
                sb.Clear();
                return null;
            }
            
            //var compta = SingletonConnection.Instance.Gescom.CptaApplication;
            /*foreach (IBOClient3 client3 in SingletonConnection.Instance.Compta.FactoryClient.List)
            {
                if (client3.Telecom.EMail.ToUpper().Equals(email.ToUpper()))
                {
                    return client3.CT_Num;
                }
                else
                {
                    foreach (IBOTiersContact3 contact3 in client3.FactoryTiersContact.List)
                    {
                        if (contact3.Telecom.EMail.ToUpper().Equals(email.ToUpper()))
                        {
                            return client3.CT_Num;
                        }
                    }
                }
            }*/
        }
    }
}
