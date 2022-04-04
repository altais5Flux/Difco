using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using WebservicesSage.Utils;
using WebservicesSage.Utils.Enums;
using WebservicesSage.Singleton;
using Objets100cLib;
using WebservicesSage.Object;
using WebservicesSage.Object.DBObject;
using System.Windows.Forms;
using LiteDB;
using System.IO;
using Newtonsoft.Json;
using WebservicesSage.Object.Order;
using WebservicesSage.Object.CustomerSearch;
using System.Globalization;
using WebservicesSage.Object.Devis;
using Customer = WebservicesSage.Object.Customer;
using WebservicesSage.Object.CustomerSearchByEmail;
using System.Data.SqlClient;

namespace WebservicesSage.Cotnroller
{
    public static class ControllerCommande
    {

        /// <summary>
        /// Lance le service de check des nouvelles commandes prestashop
        /// Définir le temps de passage de la tâche dans la config
        /// </summary>
        public static void LaunchService()
        {
            //SingletonUI.Instance.LogBox.Invoke((MethodInvoker)(() => SingletonUI.Instance.LogBox.AppendText("Commande Services Launched " + Environment.NewLine)));
            
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Elapsed += new ElapsedEventHandler(CheckForNewOrderMagentoCron);
            timer.Interval = UtilsConfig.CronTaskCheckForNewOrder;
            timer.Enabled = true;
            
            System.Timers.Timer timerUpdateStatut = new System.Timers.Timer();
            //timerUpdateStatut.Elapsed += new ElapsedEventHandler(UpdateStatuOrder);
            timerUpdateStatut.Interval = UtilsConfig.CronTaskUpdateStatut;
            timerUpdateStatut.Enabled = true;

            
        }

        public static void CheckForNewOrderMagentoCron(object source, ElapsedEventArgs e)
        {
            CheckForNewOrderMagento();
            CheckForNewOrderMagentoFlag2();
        }

        /// <summary>
        /// Event levé par une nouvelle commande dans prestashop
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        /// 

        public static void CheckForNewOrderMagentoFlag2()
        {

            try
            {
                string response = UtilsWebservices.SearchOrder(UtilsConfig.BaseUrl + "/rest/V1/orders", UtilsWebservices.SearchOrderCriteria("order_flag", "2", "eq"));
                OrderSearch orderSearch = OrderSearch.FromJson(response);
                if (orderSearch.TotalCount > 0)
                {
                    //todo create BC sage
                    for (int i = 0; i < orderSearch.TotalCount; i++)
                    {
                        string currentIdOrder = "0";
                        string clientCtNum = "";
                        string clienttype = "";
                        string currentIncrementedId = "";
                        currentIdOrder = orderSearch.Items[i].EntityId.ToString();
                        currentIncrementedId = orderSearch.Items[i].IncrementId.ToString();
                        try
                        {

                            if (orderSearch.Items[i].Status.Equals("canceled") || orderSearch.Items[i].Status.Equals("fraud") || orderSearch.Items[i].State.Equals("fraud") || orderSearch.Items[i].State.Equals("canceled"))
                            {
                                var jsonFlag = JsonConvert.SerializeObject(UpdateOrderFlag(currentIdOrder, currentIncrementedId, "3"));
                                UtilsWebservices.SendDataJson(jsonFlag, @"rest/all/V1/orders/", "POST");
                                continue;
                            }
                            if (orderSearch.Items[i].Status.Equals("pending_payment") || orderSearch.Items[i].State.Equals("pending_payment") || orderSearch.Items[i].State.Equals("pending") || orderSearch.Items[i].Status.Equals("pending"))
                            {
                                continue;
                            }
                            CustomerSearch client = UtilsWebservices.GetClientCtNum(orderSearch.Items[i].CustomerId.ToString());

                            try
                            {
                                for (int j = 0; j < client.CustomAttributes.Count; j++)
                                {
                                    if (client.CustomAttributes[j].AttributeCode.Equals("sage_number"))
                                    {
                                        clientCtNum = client.CustomAttributes[j].Value.ToString();
                                        //File.AppendAllText("Log\\GetOrderCustomer.txt", "Client CT_NUM : " + clientCtNum + Environment.NewLine);
                                    }
                                    /*if (client.CustomAttributes[j].AttributeCode.Equals("customer_type"))
                                    {
                                        clienttype = client.CustomAttributes[j].Value.ToString();
                                        //File.AppendAllText("Log\\GetOrderCustomer.txt", "Client type : "+clienttype + Environment.NewLine);
                                    }*/
                                }
                                clientCtNum = UtilsConfig.PrefixClient + client.Id.ToString();
                            }
                            catch (Exception exception)
                            {

                                clientCtNum = "";
                            }
                            if (!String.IsNullOrEmpty(clientCtNum.ToUpper()) && ControllerClient.CheckIfClientExist(clientCtNum.ToUpper()))
                            {
                                if (ControllerClient.CheckIfClientExist(clientCtNum.ToUpper()))
                                {
                                    // si le client existe on associé la commande à son compte
                                    AddNewOrderForCustomer(orderSearch.Items[i], clientCtNum.ToUpper(), client);
                                }
                            }
                            else
                            {
                                string debugClient = UtilsWebservices.GetMagentoData("rest/V1/customers/search" + UtilsWebservices.SearchOrderCriteria("email", client.Email, "eq"));
                                File.AppendAllText("Log\\GetCustomer.txt", debugClient.ToString() + Environment.NewLine);
                                CustomerSearchByEmail ClientSearch = CustomerSearchByEmail.FromJson(UtilsWebservices.GetMagentoData("rest/V1/customers/search" + UtilsWebservices.SearchOrderCriteria("email", client.Email, "eq")));
                                Customer customerMagento = new Customer();
                                string clientSageObj = ControllerClient.CheckIfClientEmailExist(client.Email);
                                if (!String.IsNullOrEmpty(clientSageObj))
                                {
                                    IBOClient3 customerSage = SingletonConnection.Instance.Gescom.CptaApplication.FactoryClient.ReadNumero(clientSageObj);
                                    Client ClientData = new Client(customerSage);
                                    var jsonClient = JsonConvert.SerializeObject(customerMagento.UpdateCustomer(clientSageObj, clienttype, client));
                                    UtilsWebservices.SendDataJson(jsonClient, @"rest/all/V1/customers/" + ClientSearch.Items[0].Id.ToString(), "PUT");
                                    AddNewOrderForCustomer(orderSearch.Items[i], clientSageObj, client);
                                }
                                else
                                {
                                    string ct_num = ControllerClient.CreateNewClient(client, orderSearch.Items[i]);

                                    if (!String.IsNullOrEmpty(ct_num))
                                    {
                                        var jsonClient = JsonConvert.SerializeObject(customerMagento.UpdateCustomer(ct_num, clienttype, client));
                                        UtilsWebservices.SendDataJson(jsonClient, @"rest/all/V1/customers/" + client.Id.ToString(), "PUT");
                                        // le client à bien été crée on peut intégrer la commande sur son compte sage
                                        AddNewOrderForCustomer(orderSearch.Items[i], ct_num, client);
                                    }
                                    else
                                    {
                                        File.AppendAllText("Log\\order.txt", DateTime.Now + "erreur creation du client" + Environment.NewLine);
                                        File.AppendAllText("Log\\order.txt", DateTime.Now + "Erreur avec la commande : " + currentIncrementedId + Environment.NewLine);
                                        var jsonFlag = JsonConvert.SerializeObject(UpdateOrderFlag(currentIdOrder, currentIncrementedId, "2"));
                                        UtilsWebservices.SendDataJson(jsonFlag, @"rest/all/V1/orders/", "POST");
                                    }
                                }
                            }

                        }
                        catch (Exception s)
                        {
                            var jsonFlag = JsonConvert.SerializeObject(UpdateOrderFlag(currentIdOrder, currentIncrementedId, "2"));
                            UtilsWebservices.SendDataJson(jsonFlag, @"rest/all/V1/orders/", "POST");
                            StringBuilder sb = new StringBuilder();
                            sb.Append(DateTime.Now + "Erreur avec la commande : " + currentIncrementedId + Environment.NewLine);
                            sb.Append(DateTime.Now + s.Message + Environment.NewLine);
                            sb.Append(DateTime.Now + s.StackTrace + Environment.NewLine);
                            File.AppendAllText("Log\\order.txt", sb.ToString());
                            sb.Clear();

                        }
                    }
                }
            }
            catch (Exception s)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now + s.Message + Environment.NewLine);
                sb.Append(DateTime.Now + s.StackTrace + Environment.NewLine);
                File.AppendAllText("Log\\order.txt", sb.ToString());
                sb.Clear();
                //var jsonFlag = JsonConvert.SerializeObject(UpdateOrderFlag(currentIdOrder, currentIncrementedId, "2"));
                //UtilsWebservices.SendDataJson(jsonFlag, @"rest/all/V1/orders/", "POST");
                //UtilsWebservices.SendDataNoParse(UtilsConfig.BaseUrl + EnumEndPoint.Commande.Value, "errorOrder&orderID=" + currentIdOrder);
            }

        }


        public static void CheckForNewOrderMagento()
        {

            try
            {
                string response = UtilsWebservices.SearchOrder(UtilsConfig.BaseUrl + "/rest/V1/orders", UtilsWebservices.SearchOrderCriteria("order_flag", UtilsConfig.Flag.ToString(), "eq"));
                OrderSearch orderSearch = OrderSearch.FromJson(response);
                File.AppendAllText("Log\\order.txt", orderSearch.TotalCount.ToString());
                if (orderSearch.TotalCount > 0)
                {
                    File.AppendAllText("Log\\order.txt", orderSearch.TotalCount.ToString());

                    //todo create BC sage
                    for (int i = 0; i < orderSearch.TotalCount; i++)
                    {
                        File.AppendAllText("Log\\order.txt", orderSearch.TotalCount.ToString());

                        string currentIdOrder = "0";
                        string clientCtNum = "";
                        string clienttype = "";
                        string currentIncrementedId = "";
                        currentIdOrder = orderSearch.Items[i].EntityId.ToString();
                        currentIncrementedId = orderSearch.Items[i].IncrementId.ToString();
                        try
                        {

                            if (orderSearch.Items[i].Status.Equals("canceled") || orderSearch.Items[i].Status.Equals("fraud") || orderSearch.Items[i].State.Equals("fraud") || orderSearch.Items[i].State.Equals("canceled"))
                            {
                                var jsonFlag = JsonConvert.SerializeObject(UpdateOrderFlag(currentIdOrder, currentIncrementedId, "3"));
                                UtilsWebservices.SendDataJson(jsonFlag, @"rest/all/V1/orders/", "POST");
                                continue;
                            }
                            if (orderSearch.Items[i].Status.Equals("pending_payment") || orderSearch.Items[i].State.Equals("pending_payment") || orderSearch.Items[i].State.Equals("pending") || orderSearch.Items[i].Status.Equals("pending"))
                            {
                                continue;
                            }
                            CustomerSearch client = UtilsWebservices.GetClientCtNum(orderSearch.Items[i].CustomerId.ToString());
                            File.AppendAllText("Log\\order.txt", client.Id.ToString());

                            try
                            {
                                for (int j = 0; j < client.CustomAttributes.Count; j++)
                                {
                                    if (client.CustomAttributes[j].AttributeCode.Equals("sage_number"))
                                    {
                                        clientCtNum = client.CustomAttributes[j].Value.ToString();
                                        //File.AppendAllText("Log\\GetOrderCustomer.txt", "Client CT_NUM : " + clientCtNum + Environment.NewLine);
                                    }
                                    /*if (client.CustomAttributes[j].AttributeCode.Equals("customer_type"))
                                    {
                                        clienttype = client.CustomAttributes[j].Value.ToString();
                                        //File.AppendAllText("Log\\GetOrderCustomer.txt", "Client type : "+clienttype + Environment.NewLine);
                                    }*/
                                }
                                clientCtNum = UtilsConfig.PrefixClient + client.Id.ToString();
                            }
                            catch (Exception exception)
                            {

                                clientCtNum = "";
                            }
                            if (!String.IsNullOrEmpty(clientCtNum.ToUpper()) && ControllerClient.CheckIfClientExist(clientCtNum.ToUpper()))
                            {
                                File.AppendAllText("Log\\order.txt", Environment.NewLine + clientCtNum.ToUpper());

                                if (ControllerClient.CheckIfClientExist(clientCtNum.ToUpper()))
                                {
                                    // si le client existe on associé la commande à son compte
                                    AddNewOrderForCustomer(orderSearch.Items[i], clientCtNum.ToUpper(), client);
                                }
                            }
                            else
                            {
                                string debugClient = UtilsWebservices.GetMagentoData("rest/V1/customers/search" + UtilsWebservices.SearchOrderCriteria("email", client.Email, "eq"));
                                File.AppendAllText("Log\\GetCustomer.txt", debugClient.ToString() + Environment.NewLine);
                                CustomerSearchByEmail ClientSearch = CustomerSearchByEmail.FromJson(UtilsWebservices.GetMagentoData("rest/V1/customers/search" + UtilsWebservices.SearchOrderCriteria("email", client.Email, "eq")));
                                Customer customerMagento = new Customer();
                                string clientSageObj = ControllerClient.CheckIfClientEmailExist(client.Email);
                                if (!String.IsNullOrEmpty(clientSageObj))
                                {
                                    IBOClient3 customerSage = SingletonConnection.Instance.Gescom.CptaApplication.FactoryClient.ReadNumero(clientSageObj);
                                    Client ClientData = new Client(customerSage);
                                    var jsonClient = JsonConvert.SerializeObject(customerMagento.UpdateCustomer(clientSageObj, clienttype, client));
                                    UtilsWebservices.SendDataJson(jsonClient, @"rest/all/V1/customers/" + ClientSearch.Items[0].Id.ToString(), "PUT");
                                    AddNewOrderForCustomer(orderSearch.Items[i], clientSageObj, client);
                                }
                                else
                                {
                                    string ct_num = ControllerClient.CreateNewClient(client, orderSearch.Items[i]);

                                    if (!String.IsNullOrEmpty(ct_num))
                                    {
                                        var jsonClient = JsonConvert.SerializeObject(customerMagento.UpdateCustomer(ct_num, clienttype, client));
                                        UtilsWebservices.SendDataJson(jsonClient, @"rest/all/V1/customers/" + client.Id.ToString(), "PUT");
                                        // le client à bien été crée on peut intégrer la commande sur son compte sage
                                        AddNewOrderForCustomer(orderSearch.Items[i], ct_num, client);
                                    }
                                    else
                                    {
                                        File.AppendAllText("Log\\order.txt", DateTime.Now + "erreur creation du client" + Environment.NewLine);
                                        File.AppendAllText("Log\\order.txt", DateTime.Now + "Erreur avec la commande : " + currentIncrementedId + Environment.NewLine);
                                        var jsonFlag = JsonConvert.SerializeObject(UpdateOrderFlag(currentIdOrder, currentIncrementedId, "2"));
                                        UtilsWebservices.SendDataJson(jsonFlag, @"rest/all/V1/orders/", "POST");
                                    }
                                }
                            }

                        }
                        catch (Exception s)
                        {
                            var jsonFlag = JsonConvert.SerializeObject(UpdateOrderFlag(currentIdOrder, currentIncrementedId, "2"));
                            UtilsWebservices.SendDataJson(jsonFlag, @"rest/all/V1/orders/", "POST");
                            StringBuilder sb = new StringBuilder();
                            sb.Append(DateTime.Now + "Erreur avec la commande : " + currentIncrementedId + Environment.NewLine);
                            sb.Append(DateTime.Now + s.Message + Environment.NewLine);
                            sb.Append(DateTime.Now + s.StackTrace + Environment.NewLine);
                            File.AppendAllText("Log\\order.txt", sb.ToString());
                            sb.Clear();

                        }
                    }
                }
            }
            catch (Exception s)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now + s.Message + Environment.NewLine);
                sb.Append(DateTime.Now + s.StackTrace + Environment.NewLine);
                File.AppendAllText("Log\\order.txt", sb.ToString());
                sb.Clear();
                //var jsonFlag = JsonConvert.SerializeObject(UpdateOrderFlag(currentIdOrder, currentIncrementedId, "2"));
                //UtilsWebservices.SendDataJson(jsonFlag, @"rest/all/V1/orders/", "POST");
                //UtilsWebservices.SendDataNoParse(UtilsConfig.BaseUrl + EnumEndPoint.Commande.Value, "errorOrder&orderID=" + currentIdOrder);
            }

        }

        /*public static void UpdateStatuOrder(object source, ElapsedEventArgs e)
        {
            string test = "";
            try
            {
                var gescom = SingletonConnection.Instance.Gescom;
                var compta = SingletonConnection.Instance.Compta;
                
                //IBICollection AllOrders = gescom.FactoryDocumentVente.List;
                using (var db = new LiteDatabase(@"MyData.db"))
                {
                    // Get OrderMapping from Config

                    string MagentoStatutId, orderStatutId, statut1, statut2, statut3;
                    string[] MagentoID, orderStatut;
                    UtilsConfig.MagentoStatutId.TryGetValue("default", out MagentoStatutId);
                    MagentoID = MagentoStatutId.Split('_');
                    string statutMagento1, statutMagento2, statutMagento3;
                    UtilsConfig.MagentoStatutId.TryGetValue(MagentoID[0], out statutMagento1);
                    UtilsConfig.MagentoStatutId.TryGetValue(MagentoID[1], out statutMagento2);
                    UtilsConfig.MagentoStatutId.TryGetValue(MagentoID[2], out statutMagento3);
                    UtilsConfig.OrderMapping.TryGetValue("default", out orderStatutId);
                    orderStatut = orderStatutId.Split('_');
                    UtilsConfig.OrderMapping.TryGetValue(orderStatut[0], out statut1);
                    UtilsConfig.OrderMapping.TryGetValue(orderStatut[1], out statut2);
                    UtilsConfig.OrderMapping.TryGetValue(orderStatut[2], out statut3);
                    //statut1 =UtilsConfig.OrderMapping. //orderStatut[0];
                    //statut2 = orderStatut[1];
                    //statut3 = orderStatut[2];

                    // Get a collection (or create, if doesn't exist)
                    var col = db.GetCollection<LinkedCommandeDB>("Commande");
                    foreach (LinkedCommandeDB item in col.FindAll())
                    {
                        DocumentType OrderDocumentType = DocumentType.DocumentTypeVenteCommande;
                        string sql = "SELECT DO_Type FROM [" + System.Configuration.ConfigurationManager.AppSettings["DBNAME"].ToString() + "].[dbo].[F_DOCENTETE] WHERE DO_Ref = '" + item.DO_Ref + "'"; ;
                        File.AppendAllText("Log\\SQL.txt", DateTime.Now + sql.ToString() + Environment.NewLine);
                        SqlDataReader orderType = DB.Select(sql);
                        while (orderType.Read())
                        {

                            File.AppendAllText("Log\\SQL.txt", DateTime.Now + orderType.GetValue(0).ToString() + Environment.NewLine);
                            if (orderType.GetValue(0).ToString().Equals("2"))
                            {
                                OrderDocumentType = DocumentType.DocumentTypeVentePrepaLivraison;
                            }
                            else if (orderType.GetValue(0).ToString().Equals("3"))
                            {
                                OrderDocumentType = DocumentType.DocumentTypeVenteLivraison;
                            }
                            else if (orderType.GetValue(0).ToString().Equals("6"))
                            {
                                OrderDocumentType = DocumentType.DocumentTypeVenteFacture;
                            }
                            else if (orderType.GetValue(0).ToString().Equals("7"))
                            {
                                OrderDocumentType = DocumentType.DocumentTypeVenteFactureCpta;
                            }
                        }
                        test = item.OrderID;
                        if (OrderDocumentType.ToString().Equals("DocumentTypeVenteCommande"))
                        {
                            continue;
                        }
                        else
                        {
                            if (OrderDocumentType.ToString().Equals(statut1.Split('_')[0]))
                            {
                                UtilsWebservices.SendDataJson(UpdateStatusOnMagento(item.OrderID, item.incremented_id, statutMagento1), @"rest/V1/orders");
                                item.OrderType = statut1.Split('_')[0];
                                col.Update(item);
                                //col.Update()
                                File.AppendAllText("Log\\statut.txt", DateTime.Now + " stat1  " + item.DO_Ref + " " + item.OrderType + Environment.NewLine);
                                continue;
                            }
                            if (OrderDocumentType.ToString().Equals(statut2.Split('_')[0]))
                            {
                                UtilsWebservices.SendDataJson(UpdateStatusOnMagento(item.OrderID, item.incremented_id, statutMagento2), @"rest/V1/orders");
                                item.OrderType = statut2.Split('_')[0];
                                col.Update(item);
                                File.AppendAllText("Log\\statut.txt", DateTime.Now + " stat2  " + item.DO_Ref +" "+ item.OrderType + Environment.NewLine);
                                //col.Update()
                                continue;
                            }
                            if (OrderDocumentType.ToString().Equals(statut3.Split('_')[0]))
                            {
                                UtilsWebservices.SendDataJson(UpdateStatusOnMagento(item.OrderID, item.incremented_id, statutMagento3), @"rest/V1/orders");
                                col.Delete(item.Id);
                                File.AppendAllText("Log\\statut.txt", DateTime.Now + " stat3  " + item.DO_Ref + " " + item.OrderType + Environment.NewLine);
                                continue;
                            }
                        }
                        DB.Disconnect(); 
                    }
                }
            }
            catch(Exception s)
            {
                UtilsMail.SendErrorMail(DateTime.Now + s.Message + Environment.NewLine + s.StackTrace + Environment.NewLine, "UPDATE STATUT ORDER "+ test);
            }
        }*/

        /// <summary>
        /// Crée une nouvelle commande pour un utilisateur
        /// </summary>
        /// <param name="jsonOrder">Order à crée</param>
        /// <param name="CT_Num">Client</param>
        public static void AddNewOrderForCustomer(OrderSearchItem orderMagento, string CT_Num,CustomerSearch customerMagento)
        {
            File.AppendAllText("Log\\Commande.txt",DateTime.Now + "Begin Creation de commande : " + orderMagento.IncrementId.ToString() + Environment.NewLine);
            var gescom = SingletonConnection.Instance.Gescom;

            // création de l'entête de la commande 

            IBOClient3 customer = gescom.CptaApplication.FactoryClient.ReadNumero(CT_Num);
            IBODocumentVente3 order = gescom.FactoryDocumentVente.CreateType(DocumentType.DocumentTypeVenteCommande);
            order.SetDefault();
            order.SetDefaultClient(customer);
            order.DO_Date = DateTime.Now;

            // TODO Manage Order Carrier
            string code_relais = "";
            try
            {
                code_relais = orderMagento.ExtensionAttributes.DpdPickupId.ToString()+" ";
            }
            catch (Exception exception)
            {
                code_relais = "";
            }
            string carrier_id = "default";
            try
            {
                
                
                if (orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Method.Equals("owsh1_shipping_matpro"))
                {
                    carrier_id = "1";
                    
                }
                if (orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Method.Equals("dpd_classic") || orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Method.Equals("dpd_predict") || orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Method.Equals("dpd_pickup"))
                {
                    carrier_id = "2";
                }
                if (orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Method.Equals("owsh1_shipping_geodis")|| orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Method.Equals("owebiashipping1_Geodis"))
                {
                    carrier_id = "3";
                }
                if (orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Method.Equals("lengow_lengow")|| orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Method.Equals("owebiashipping3_chronopost0"))
                {
                    carrier_id = "4";
                }
                if (orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Method.Equals("dpdfrpredict_code_auto003")|| orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Method.Equals("dpdfrpredict_code_auto001"))
                {
                    carrier_id = "5";

                }
                
                order.Expedition = gescom.FactoryExpedition.ReadIntitule(UtilsConfig.OrderCarrierMapping[carrier_id]);
            }
            catch (Exception s)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now + "problème order expidition" + Environment.NewLine);
                sb.Append(DateTime.Now + s.Message + Environment.NewLine);
                sb.Append(DateTime.Now + s.StackTrace + Environment.NewLine);
                UtilsMail.SendErrorMail(DateTime.Now + s.Message + Environment.NewLine + s.StackTrace + Environment.NewLine, "TRANSPORTEUR");
                File.AppendAllText("Log\\order.txt", sb.ToString());
                sb.Clear();
            }
            order.Souche = gescom.FactorySoucheVente.ReadIntitule(UtilsConfig.Souche);
            order.DO_Ref = orderMagento.IncrementId.ToString() ;
            order.SetDefaultDO_Piece();
            
            order.Write();
            
            try
            {
                if (orderMagento.Payment.Method.Equals("banktransfer"))
                {
                    /*
                    IBODocumentAcompte3 acompte = (IBODocumentAcompte3)order.FactoryDocumentAcompte.Create();
                    if (orderMagento.TotalInvoiced > 0)
                    {
                        acompte.DR_Montant = orderMagento.TotalInvoiced;
                    }
                    else
                    {
                        acompte.DR_Montant = orderMagento.TotalDue;
                    }
                    
                    acompte.DR_Date = DateTime.Now;
                    foreach (IBPReglement3 reglement in gescom.CptaApplication.FactoryReglement.List)
                    {
                        if (reglement.R_Intitule.Equals("Virement"))
                        {
                            acompte.Reglement = reglement;
                            break;
                        }
                    }
                    
                    acompte.DR_Libelle = "Acompte";
                    acompte.Write();*/
                    order.InfoLibre[1] ="Virement";
                    order.Write();
                }
                else if (orderMagento.Payment.Method.Equals("paypal_express"))
                {
                    /*IBODocumentAcompte3 acompte = (IBODocumentAcompte3)order.FactoryDocumentAcompte.Create();
                    if (orderMagento.TotalInvoiced > 0)
                    {
                        acompte.DR_Montant = orderMagento.TotalInvoiced;
                    }
                    else
                    {
                        acompte.DR_Montant = orderMagento.TotalDue;
                    }
                    acompte.DR_Date = DateTime.Now;
                    foreach (IBPReglement3 reglement in gescom.CptaApplication.FactoryReglement.List)
                    {
                        if (reglement.R_Intitule.Equals("Paypal"))
                        {
                            acompte.Reglement = reglement;
                            break;
                        }
                    }

                    acompte.DR_Libelle = "Acompte";
                    acompte.Write();*/
                    order.InfoLibre[1] = "Paypal";
                    order.Write();
                }else if (orderMagento.Payment.Method.Equals("systempay_standard"))
                {
                    /*IBODocumentAcompte3 acompte = (IBODocumentAcompte3)order.FactoryDocumentAcompte.Create();
                    if (orderMagento.TotalInvoiced > 0)
                    {
                        acompte.DR_Montant = orderMagento.TotalInvoiced;
                    }
                    else
                    {
                        acompte.DR_Montant = orderMagento.TotalDue;
                    }
                    acompte.DR_Date = DateTime.Now;
                    foreach (IBPReglement3 reglement in gescom.CptaApplication.FactoryReglement.List)
                    {
                        if (reglement.R_Intitule.Equals("Carte bancaire"))
                        {
                            acompte.Reglement = reglement;
                            break;
                        }
                    }

                    acompte.DR_Libelle = "Acompte";
                    acompte.Write();*/
                    order.InfoLibre[1] = "Carte bancaire";

                    order.Write();
                }
            }
            catch (Exception exc)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now + "probleme payement : "+orderMagento.IncrementId.ToString() + Environment.NewLine);
                sb.Append(DateTime.Now + exc.Message + Environment.NewLine);
                sb.Append(DateTime.Now + exc.StackTrace + Environment.NewLine);
                File.AppendAllText("Log\\acompte.txt", sb.ToString());
                sb.Clear();
            }
            
            bool asAdressMatch = false;
                IBOClientLivraison3 currentAdress = null;

                foreach (IBOClientLivraison3 tmpAdress in customer.FactoryClientLivraison.List)
                {
                    if (!String.IsNullOrEmpty(orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.company))
                    {
                        if (tmpAdress.LI_Intitule.Equals(code_relais + orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.company.ToUpper()))
                        {
                            currentAdress = tmpAdress;
                            asAdressMatch = true;
                            break;
                        }
                    }
                    string intitule = "";
                    intitule = code_relais + orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Firstname + " " + orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Lastname;
                    if (intitule.Length > 35)
                    {
                        intitule = intitule.Substring(0, 35);
                    }
                    if (tmpAdress.LI_Intitule.Equals(intitule.ToUpper()))
                    {
                        currentAdress = tmpAdress;
                        asAdressMatch = true;
                        break;
                    }

                }
           
            try
                { 
            // si on a trouver aucune adresse coresspondante sur le client alors on la crée
            IBOClientLivraison3 adress;
            if (!asAdressMatch)
            {
                adress = (IBOClientLivraison3)customer.FactoryClientLivraison.Create();
                adress.SetDefault();
            }
            else
            {
                adress = currentAdress;
            }

            adress.Telecom.EMail = customer.Telecom.EMail;
                /*
                try
                {
                    string carrier_id = jsonOrder["order_carriere"].ToString();
                    adress.Expedition = gescom.FactoryExpedition.ReadIntitule(UtilsConfig.OrderCarrierMapping[carrier_id]);
                }

                */
            if (!String.IsNullOrEmpty(orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.company))
            {
                if (code_relais.Length + orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.company.Length > 35)
                    {
                        adress.LI_Intitule = code_relais + orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.company.ToUpper().Substring(0, 35 - code_relais.Length);
                    }
                    else
                    {
                        adress.LI_Intitule = code_relais + orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.company.ToUpper();
                    }
                }
            else
            {
                string intitule = "";
                intitule = code_relais + orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Firstname + " " + orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Lastname;
                if (intitule.Length > 35)
                {
                        intitule = intitule.Substring(0, 35);
                }
                adress.LI_Intitule = intitule.ToUpper();
            }

            // Setup champ contact dans adress
            if ((orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Firstname + " " + orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Lastname).Length > 35)
            {
                adress.LI_Contact = (orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Firstname + " " + orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Lastname).Substring(0, 35);
            }
            else
            {
                adress.LI_Contact = orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Firstname + " " + orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Lastname;
            }

            if (orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Street[0].ToString().Length > 35)
            {
                adress.Adresse.Adresse = orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Street[0].ToString().Substring(0, 35);
            }
            else
            {
                adress.Adresse.Adresse = orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Street[0].ToString();
            }
            if (orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Street.Count >1)
            {
                if (orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Street[1].ToString().Length >35)
                {
                    adress.Adresse.Complement = orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Street[1].ToString().Substring(0, 35);
                }
                else
                {
                    adress.Adresse.Complement = orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Street[1].ToString();
                }
                
            }
            
            adress.Adresse.CodePostal = orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Postcode.ToString();
            adress.Adresse.Ville = orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.City.ToString();
            var region = CultureInfo
                                    .GetCultures(CultureTypes.SpecificCultures)
                                    .Select(ci => new RegionInfo(ci.LCID))
                                    .FirstOrDefault(rg => rg.TwoLetterISORegionName == orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.CountryId);
            adress.Adresse.Pays = region.DisplayName.ToString();
            adress.Telecom.Telephone = orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Address.Telephone;
            //adress.Telecom.Telecopie = jsonOrder["shipping_phone_mobile"].ToString();

            if (String.IsNullOrEmpty(UtilsConfig.CondLivraison))
            {
                // pas de configuration renseigner pour CondLivraison par defaut
                // todo log
            }
            else
            {
                adress.ConditionLivraison = gescom.FactoryConditionLivraison.ReadIntitule(UtilsConfig.CondLivraison);
            }
                try
                {
                    adress.Expedition = gescom.FactoryExpedition.ReadIntitule(UtilsConfig.OrderCarrierMapping[carrier_id]);
                }
                catch (Exception s)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append(DateTime.Now + "problème order expidition" + Environment.NewLine);
                    sb.Append(DateTime.Now + s.Message + Environment.NewLine);
                    sb.Append(DateTime.Now + s.StackTrace + Environment.NewLine);
                    UtilsMail.SendErrorMail(DateTime.Now + s.Message + Environment.NewLine + s.StackTrace + Environment.NewLine, "TRANSPORTEUR");
                    File.AppendAllText("Log\\order.txt", sb.ToString());
                    sb.Clear();
                }
            adress.Write();

                // on ajoute une adresse par defaut sur la fiche client si il y en a pas

                // On met à jour l'adresse de facturation du client
                #region Update invoice Adress
                /*
                    if (customerMagento.HasMethod("DefaultBilling"))
                    {
                        Object.CustomerSearch.Address defaultAddress = new Object.CustomerSearch.Address();
                        foreach (Object.CustomerSearch.Address addressCustomer in customerMagento.Addresses)
                        {
                            if (addressCustomer.Id == customerMagento.DefaultBilling)
                            {
                                defaultAddress = addressCustomer;
                                break;
                            }
                        }
                        if (defaultAddress.Street[0].Length > 35)
                        {
                            customer.Adresse.Adresse = defaultAddress.Street[0].ToString().Substring(0, 35);
                        }
                        else
                        {
                            customer.Adresse.Adresse = defaultAddress.Street[0].ToString();
                        }
                        if (defaultAddress.Street.Count > 1)
                        {
                            customer.Adresse.Complement = defaultAddress.Street[1].ToString();
                        }

                        customer.Adresse.CodePostal = defaultAddress.Postcode.ToString();
                        customer.Adresse.Ville = defaultAddress.City.ToString();
                        var region1 = CultureInfo
                                        .GetCultures(CultureTypes.SpecificCultures)
                                        .Select(ci => new RegionInfo(ci.LCID))
                                        .FirstOrDefault(rg => rg.TwoLetterISORegionName == defaultAddress.CountryId);
                        customer.Adresse.Pays = region1.DisplayName.ToString();
                        customer.Telecom.Telephone = defaultAddress.Telephone.ToString();
                        customer.Write();
                    }
                    else
                    {*/
                if (orderMagento.BillingAddress.Street[0].ToString().Length >35)
                {
                    customer.Adresse.Adresse = orderMagento.BillingAddress.Street[0].ToString().Substring(0, 35);
                }
                else
                {
                    customer.Adresse.Adresse = orderMagento.BillingAddress.Street[0].ToString();
                }
                if (orderMagento.BillingAddress.Street.Count > 1)
                {
                    if (orderMagento.BillingAddress.Street[1].ToString().Length >35)
                    {
                        customer.Adresse.Complement = orderMagento.BillingAddress.Street[1].ToString().Substring(0,35) ;
                    }
                    else
                    {
                        customer.Adresse.Complement = orderMagento.BillingAddress.Street[1].ToString();
                    }
                    
                }
                /*if (!String.IsNullOrEmpty(adress.Adresse.Complement.ToString()))
                {
                    customer.Adresse.Complement = orderMagento.BillingAddress.c //adress.Adresse.Complement.ToString();
                }*/
                var regionClient = CultureInfo
                                    .GetCultures(CultureTypes.SpecificCultures)
                                    .Select(ci => new RegionInfo(ci.LCID))
                                    .FirstOrDefault(rg => rg.TwoLetterISORegionName == orderMagento.BillingAddress.CountryId);
                    customer.Adresse.CodePostal = orderMagento.BillingAddress.Postcode.ToString(); //adress.Adresse.CodePostal.ToString();
                    customer.Adresse.Ville = orderMagento.BillingAddress.City.ToString(); //adress.Adresse.Ville.ToString();
                    customer.Adresse.Pays = regionClient.DisplayName.ToString(); // adress.Adresse.Pays.ToString();
                    customer.Telecom.Telephone = orderMagento.BillingAddress.Telephone.ToString(); // adress.Telecom.Telephone.ToString();
                    customer.Write();
                //}
            
            #endregion
            //order.FraisExpedition = orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Total.ShippingAmount;
            order.LieuLivraison = adress;
            }
            catch (Exception s)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now + "problem in customer adress" + Environment.NewLine);
                sb.Append(DateTime.Now + s.Message + Environment.NewLine);
                sb.Append(DateTime.Now + s.StackTrace + Environment.NewLine);
                File.AppendAllText("Log\\order.txt", sb.ToString());
                sb.Clear();
            }
            order.Write();
            /*try
            {
                //take care of infolibre commande
                if (orderMagento.StatusHistories.Count >0)
                {
                    foreach (StatusHistory statusHistory in orderMagento.StatusHistories)
                    {
                        if (String.IsNullOrEmpty(statusHistory.Status) &&statusHistory.IsVisibleOnFront.ToString().Equals("1") && !String.IsNullOrEmpty(statusHistory.Comment) && statusHistory.CreatedAt.Equals(orderMagento.CreatedAt))
                        {
                            order.InfoLibre[3] = statusHistory.Comment;
                        }
                    }
                    //order.InfoLibre[3] = orderMagento.StatusHistories[0]. Payment.Po_number.ToString();
                    order.Write();
                }
            }
            catch (Exception exception)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now + "probleme status histories" + Environment.NewLine);
                sb.Append(DateTime.Now + exception.Message + Environment.NewLine);
                sb.Append(DateTime.Now + exception.StackTrace + Environment.NewLine);
                File.AppendAllText("Log\\order.txt", sb.ToString());
                sb.Clear();
            }
            */
            // création des lignes de la commandes
            try
            {
                
                foreach (Object.Order.ParentItemElement product in orderMagento.Items)
                {
                    if (product.ProductType.Equals("configurable"))
                    {
                        continue;
                    }
                    IBODocumentVenteLigne3 docLigne = (IBODocumentVenteLigne3)order.FactoryDocumentLigne.Create();
                    var ArticleExist = gescom.FactoryArticle.ExistReference(product.Sku);
                    if (ArticleExist)
                    {
                        IBOArticle3 article1 = gescom.FactoryArticle.ReadReference(product.Sku.ToString());
                        Article CondArticle = new Article(article1);
                        // insertion des article à nomenclature
                        if (CondArticle.HaveNomenclature)
                        {
                            docLigne.SetDefaultArticle(article1, Int32.Parse(product.QtyOrdered.ToString()));
                            docLigne.SetDefaultRemise();
                            docLigne.Write();
                            foreach (IBOArticleNomenclature3 item in article1.FactoryArticleNomenclature.List)
                            {
                                IBODocumentVenteLigne3 docligneComposant = (IBODocumentVenteLigne3)order.FactoryDocumentLigne.Create();
                                double qte = item.NO_Qte * Int32.Parse(product.QtyOrdered.ToString());
                                docligneComposant.SetDefaultArticle(item.ArticleComposant, qte);
                                docligneComposant.ArticleCompose = item.Article;
                                docligneComposant.SetDefaultRemise();
                                docligneComposant.Write();
                            }
                        }
                        if (CondArticle.conditionnements.Count > 0)
                        {
                            //String[] SKU = product.Sku.Split('|');
                            //IBOArticle3 article1 = gescom.FactoryArticle.ReadReference(SKU[0].ToString());
                            
                            IBOArticleCond3 articleCond3 = ControllerArticle.GetArticleConditionnementEnum(article1);
                            docLigne.SetDefaultArticleConditionnement(articleCond3, Int32.Parse(product.QtyOrdered.ToString()));
                            //docLigne.DL_PrixUnitaire = product.Price;
                        }
                        else
                        {


                            //docLigne.DL_PrixUnitaire = double.Parse(product.Price.ToString(), System.Globalization.CultureInfo.InvariantCulture);
                            // produit simple
                            docLigne.SetDefaultArticle(gescom.FactoryArticle.ReadReference(product.Sku), Int32.Parse(product.QtyOrdered.ToString()));
                            docLigne.DL_PrixUnitaire = product.Price;// double.Parse(product.Price.ToString(), System.Globalization.CultureInfo.InvariantCulture);
                        }

                    }
                    else
                    {
                        // on récupère la chaine de gammages d'un produit
                        string product_attribut_string = GetParentProductDetails(product.Sku).ToString();
                        String[] subgamme = product_attribut_string.Split('|');
                        IBOArticle3 article = gescom.FactoryArticle.ReadReference(subgamme[0].ToString());
                        
                        if (subgamme.Length == 3)
                        {
                            // produit à simple gamme9

                            IBOArticleGammeEnum3 articleEnum = ControllerArticle.GetArticleGammeEnum1(article, new Gamme(subgamme[1], subgamme[2]));
                            docLigne.SetDefaultArticleMonoGamme(articleEnum, Int32.Parse(product.QtyOrdered.ToString()));
                        }
                        else if (subgamme.Length == 5)
                        {
                            // produit à double gamme
                            IBOArticleGammeEnum3 articleEnum = ControllerArticle.GetArticleGammeEnum1(article, new Gamme(subgamme[1], subgamme[2], subgamme[3], subgamme[4]));
                            IBOArticleGammeEnum3 articleEnum2 = ControllerArticle.GetArticleGammeEnum2(article, new Gamme(subgamme[1], subgamme[2], subgamme[3], subgamme[4]));
                            docLigne.SetDefaultArticleDoubleGamme(articleEnum, articleEnum2, Int32.Parse(product.QtyOrdered.ToString()));
                        }
                        
                    }
                    docLigne.Write();
                }
                IBODocumentLigne3 docLignePort = (IBODocumentLigne3)order.FactoryDocumentLigne.Create();
                IBOArticle3 articlePort = gescom.FactoryArticle.ReadReference(UtilsConfig.DefaultTransportReference);

                docLignePort.SetDefaultArticle(articlePort, Int32.Parse("1"));
                docLignePort.DL_PrixUnitaire = orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Total.ShippingAmount;
                docLignePort.Write();
                /*IBODocumentLigne3 docLigne1 = (IBODocumentLigne3)order.FactoryDocumentLigne.Create();
                docLigne1.SetDefaultArticle(gescom.FactoryArticle.ReadReference(UtilsConfig.DefaultTransportReference), 1);
                docLigne1.DL_PrixUnitaire = Convert.ToDouble(orderMagento.ShippingAmount.ToString().Replace('.', ','));
                docLigne1.Write();*/
            }
            catch (Exception e)
            {
                var jsonFlag2 = JsonConvert.SerializeObject(UpdateOrderFlag(orderMagento.EntityId.ToString(), orderMagento.IncrementId.ToString(), "2"));
                UtilsWebservices.SendDataJson(jsonFlag2, @"rest/all/V1/orders", "POST");
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now + "problem Document line" + Environment.NewLine);
                sb.Append(DateTime.Now + e.Message + Environment.NewLine);
                sb.Append(DateTime.Now + e.StackTrace + Environment.NewLine);
                UtilsMail.SendErrorMail(DateTime.Now + e.Message + Environment.NewLine + e.StackTrace + Environment.NewLine, "COMMANDE LIGNE");
                File.AppendAllText("Log\\order.txt", sb.ToString());
                sb.Clear();
                // UtilsWebservices.SendDataNoParse(UtilsConfig.BaseUrl + EnumEndPoint.Commande.Value, "errorOrder&orderID=" + jsonOrder["id_order"]);
                order.Remove();
                return;
            }
            //order.Expedition = orderMagento.ExtensionAttributes.ShippingAssignments[0].Shipping.Total.ShippingAmount;
            //order.Write();
            File.AppendAllText("Log\\Commande.txt", DateTime.Now + "End Creation de commande : " + orderMagento.IncrementId.ToString() +" ,Client : "+ order.Client.CT_Num +" ,Do_piece : "+ order.DO_Piece + Environment.NewLine);
            addOrderToLocalDB(orderMagento.EntityId.ToString(), order.Client.CT_Num, order.DO_Piece, order.DO_Ref, orderMagento.IncrementId);
            // TODO updateOrderFlag using custom PHP script
            var jsonFlag = JsonConvert.SerializeObject(UpdateOrderFlag(orderMagento.EntityId.ToString(), orderMagento.IncrementId.ToString(), "0"));
            UtilsWebservices.SendDataJson(jsonFlag, @"rest/all/V1/orders", "POST");
            /*if (UtilsWebservices.UpdateOrderFlag(orderMagento.EntityId.ToString(),"0").Equals("error"))
            {
                UtilsMail.SendErrorMail(DateTime.Now + "Commande non importé : "+ orderMagento.EntityId.ToString() + Environment.NewLine , "COMMANDE LIGNE");
            }*/
            // on notfie prestashop que la commande à bien été crée dans SAGE
            //UtilsWebservices.SendDataNoParse(UtilsConfig.BaseUrl + EnumEndPoint.Commande.Value, "validateOrder&orderID=" + jsonOrder["id_order"]);

        }

        public static void CreateDevis(DataGridView devisList)
        {
            string clientCtNum="";
            string clienttype = "";
            foreach (DataGridViewRow item in devisList.Rows)
            {
                if (item.Cells[0].Value.ToString().Equals("True"))
                {

                    string currentIdDevis = item.Cells[1].Value.ToString(); //orderSearch.Items[i].EntityId.ToString();
                    var DevisSearch = Object.Devis.Devis.FromJson(UtilsWebservices.GetMagentoData("rest/V1/amasty_quote/search"+ UtilsWebservices.SearchOrderCriteria("quote_id",currentIdDevis,"eq")));
                    if (DevisSearch.TotalCount > 0 & !String.IsNullOrEmpty(DevisSearch.Items[0].Customer.Id.ToString()))
                    {
                        CustomerSearch client = UtilsWebservices.GetClientCtNum(DevisSearch.Items[0].Customer.Id.ToString());
                        try
                        {
                            for (int j = 0; j < client.CustomAttributes.Count; j++)
                            {
                                if (client.CustomAttributes[j].AttributeCode.Equals("sage_number"))
                                {
                                    clientCtNum = client.CustomAttributes[j].Value.ToString();
                                }
                                    if (client.CustomAttributes[j].AttributeCode.Equals("customer_type"))
                                    {
                                        clienttype = client.CustomAttributes[j].Value.ToString();
                                    }
                            }
                        }
                        catch (Exception e)
                        {

                            clientCtNum = "";
                        }
                        if (ControllerClient.CheckIfClientExist(clientCtNum))
                        {

                            // si le client existe on associé la devis à son compte
                            AddNewDevisForCustomer(DevisSearch.Items[0], clientCtNum, client);

                        }
                        else
                        {/*
                            // si le client n'existe pas on récupère les info de magento et on le crée dans la base sage 
                            //string client = UtilsWebservices.SendDataNoParse(UtilsConfig.BaseUrl + EnumEndPoint.Client.Value, "getClient&clientID=" + order["id_customer"]);
                            string ct_num = ControllerClient.CreateNewClientDevis(client, DevisSearch);
                            Object.Customer customerMagento = new Object.Customer();
                            var jsonClient = JsonConvert.SerializeObject(customerMagento.UpdateCustomer(ct_num, clienttype, client.Id.ToString()));
                            UtilsWebservices.SendDataJson(jsonClient, @"rest/all/V1/customers/" + client.Id.ToString(), "PUT");
                            if (!String.IsNullOrEmpty(ct_num))
                            {
                                // le client à bien été crée on peut intégrer la commande sur son compte sage
                                AddNewDevisForCustomer(DevisSearch.Items[0], ct_num, client);
                            }*/


                            CustomerSearchByEmail ClientSearch = CustomerSearchByEmail.FromJson(UtilsWebservices.GetMagentoData("rest/V1/customers/search" + UtilsWebservices.SearchOrderCriteria("email", client.Email, "eq")));
                            Customer customerMagento = new Customer();
                            string clientSageObj = ControllerClient.CheckIfClientEmailExist(client.Email);
                            if (!String.IsNullOrEmpty(clientSageObj))
                            {
                                IBOClient3 customerSage = SingletonConnection.Instance.Gescom.CptaApplication.FactoryClient.ReadNumero(clientSageObj);
                                Client ClientData = new Client(customerSage);
                                var jsonClient = JsonConvert.SerializeObject(customerMagento.UpdateCustomer(clientSageObj, clienttype, client));
                                UtilsWebservices.SendDataJson(jsonClient, @"rest/all/V1/customers/" + ClientSearch.Items[0].Id.ToString(), "PUT");
                                AddNewDevisForCustomer(DevisSearch.Items[0], clientSageObj, client);
                            }
                            else
                            {
                                string ct_num = ControllerClient.CreateNewClientDevis(client, DevisSearch);//.Items[0]);

                                if (!String.IsNullOrEmpty(ct_num))
                                {
                                    var jsonClient = JsonConvert.SerializeObject(customerMagento.UpdateCustomer(ct_num, clienttype, client));
                                    UtilsWebservices.SendDataJson(jsonClient, @"rest/all/V1/customers/" + client.Id.ToString(), "PUT");
                                    // le client à bien été crée on peut intégrer la commande sur son compte sage
                                    AddNewDevisForCustomer(DevisSearch.Items[0], ct_num, client);
                                }
                            }
                        }
                    }
 
                }   
               
            }
        }

        private static void AddNewDevisForCustomer(DevisItem devisItem, string ct_num, CustomerSearch client)
        {
            var gescom = SingletonConnection.Instance.Gescom;

            // création de l'entête de la commande 

            IBOClient3 customer = gescom.CptaApplication.FactoryClient.ReadNumero(ct_num);
            IBODocumentVente3 order = gescom.FactoryDocumentVente.CreateType(DocumentType.DocumentTypeVenteDevis);
            order.SetDefault();
            order.SetDefaultClient(customer);
            order.DO_Date = DateTime.Now;
            order.Souche = gescom.FactorySoucheVente.ReadIntitule(UtilsConfig.Souche);
            order.DO_Ref = "WEB " + devisItem.Id.ToString();//orderMagento.EntityId.ToString();
            order.SetDefaultDO_Piece();

            order.Write();
            // création des lignes de la commandes
            try
            {
                foreach (Object.Devis.ItemItem product in devisItem.Items)
                {
                    if (product.ProductType.Equals("configurable"))
                    {
                        continue;
                    }
                    IBODocumentLigne3 docLigne = (IBODocumentLigne3)order.FactoryDocumentLigne.Create();
                    var ArticleExist = gescom.FactoryArticle.ExistReference(product.Sku);
                    if (ArticleExist)
                    {
                        IBOArticle3 article1 = gescom.FactoryArticle.ReadReference(product.Sku.ToString());
                        Article CondArticle = new Article(article1);
                        if (CondArticle.conditionnements.Count > 0)
                        {
                            //String[] SKU = product.Sku.Split('|');
                            //IBOArticle3 article1 = gescom.FactoryArticle.ReadReference(SKU[0].ToString());

                            IBOArticleCond3 articleCond3 = ControllerArticle.GetArticleConditionnementEnum(article1);
                            docLigne.SetDefaultArticleConditionnement(articleCond3, Int32.Parse(product.Qty.ToString()));
                        }
                        else
                        {
                            docLigne.DL_PrixUnitaire = double.Parse(product.Price.ToString(), System.Globalization.CultureInfo.InvariantCulture);
                            // produit simple
                            docLigne.SetDefaultArticle(gescom.FactoryArticle.ReadReference(product.Sku), Int32.Parse(product.Qty.ToString()));
                        }
                        //SHipping price

                        /*if (product["product_ref"].ToString().Equals("TRANSPORT"))
                        {
                            docLigne.DL_PrixUnitaire = Convert.ToDouble(orderMagento.ShippingAmount.ToString().Replace('.', ','));
                        }
                        else if (product["product_ref"].ToString().Equals("REMISE"))
                        {
                            docLigne.DL_PrixUnitaire = Convert.ToDouble(product.Price.ToString().Replace('.', ','));
                        }*/
                    }
                    else
                    {
                        // on récupère la chaine de gammages d'un produit
                        string product_attribut_string = GetParentProductDetails(product.Sku).ToString();
                        String[] subgamme = product_attribut_string.Split('|');
                        IBOArticle3 article = gescom.FactoryArticle.ReadReference(subgamme[0].ToString());
                        if (subgamme.Length == 3)
                        {
                            // produit à simple gamme
                            IBOArticleGammeEnum3 articleEnum = ControllerArticle.GetArticleGammeEnum1(article, new Gamme(subgamme[1], subgamme[2]));
                            docLigne.SetDefaultArticleMonoGamme(articleEnum, Int32.Parse(product.Qty.ToString()));
                        }
                        else if (subgamme.Length == 5)
                        {
                            // produit à double gamme
                            IBOArticleGammeEnum3 articleEnum = ControllerArticle.GetArticleGammeEnum1(article, new Gamme(subgamme[1], subgamme[2], subgamme[3], subgamme[4]));
                            IBOArticleGammeEnum3 articleEnum2 = ControllerArticle.GetArticleGammeEnum2(article, new Gamme(subgamme[1], subgamme[2], subgamme[3], subgamme[4]));
                            docLigne.SetDefaultArticleDoubleGamme(articleEnum, articleEnum2, Int32.Parse(product.Qty.ToString()));
                        }
                    }
                    docLigne.Write();
                }
                
            }
            catch (Exception e)
            {
                //UtilsWebservices.UpdateOrderFlag(order.EntityId.ToString(), "2");
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now + e.Message + Environment.NewLine);
                sb.Append(DateTime.Now + e.StackTrace + Environment.NewLine);
                //UtilsMail.SendErrorMail(DateTime.Now + e.Message + Environment.NewLine + e.StackTrace + Environment.NewLine, "COMMANDE LIGNE");
                File.AppendAllText("Log\\Devis.txt", sb.ToString());
                sb.Clear();
                // UtilsWebservices.SendDataNoParse(UtilsConfig.BaseUrl + EnumEndPoint.Commande.Value, "errorOrder&orderID=" + jsonOrder["id_order"]);
                order.Remove();
                return;
            }
        }

        private static void addOrderToLocalDB(string orderID, string CT_Num, string DO_piece, string DO_Ref, string incremented_id)
        {
            // Open database (or create if doesn't exist)
            using (var db = new LiteDatabase(@"MyData.db"))
            {
                // Get a collection (or create, if doesn't exist)
                var col = db.GetCollection<LinkedCommandeDB>("Commande");

                // Create your new customer instance
                var commande = new LinkedCommandeDB
                {
                    OrderID = orderID,
                    OrderType = "DocumentTypeVenteCommande",
                    CT_Num = CT_Num,
                    DO_piece = DO_piece,
                    DO_Ref = DO_Ref,
                    incremented_id = incremented_id

                };
                col.Insert(commande);
            }
        }

        public static string GetPrestaOrderStatutFromMapping(DocumentType orderSageType)
        {
            string prestaType;
            if(UtilsConfig.OrderMapping.TryGetValue(orderSageType.ToString(), out prestaType))
            {
                return prestaType;
            }
            else
            {
                return null;
            }
        }

        public static string UpdateStatusOnMagento(string orderID, string incremented_id , string status)
        {
            var UpdateOrder = new
            {
                entity = new
                {
                    entity_id = orderID,
                    increment_id = incremented_id,
                    status = status
                }
            };
            return JsonConvert.SerializeObject(UpdateOrder);
        }
        public static object UpdateOrderFlag(string orderID, string incremented_id, string flag)
        {
            var updateFlag = new
            {
                entity = new
                {
                    entity_id = orderID,
                    increment_id = incremented_id,
                    extension_attributes = new
                    {
                        order_flag = flag
                    }
                }
            };
            return updateFlag;
        }
        public static StringBuilder GetParentProductDetails(string sku)
        {
            var gescom = SingletonConnection.Instance.Gescom;
            var articlesSageObj = gescom.FactoryArticle.List;
            StringBuilder results = new StringBuilder();
            results.Append("");
            foreach (IBOArticle3 articleSage in articlesSageObj)
            {
                // on check si l'article est cocher en publier sur le site marchand
                if (!articleSage.AR_Publie)
                    continue;
                Article article = new Article(articleSage);
                if (article.isGamme)
                {
                    foreach (Gamme doubleGamme in article.Gammes)
                    {
                        if (article.IsDoubleGamme)
                        {
                            if (doubleGamme.Reference.Equals(sku))
                            {
                                results.Append(article.Reference);
                                results.Append("|");
                                results.Append(doubleGamme.Intitule);
                                results.Append("|");
                                results.Append(doubleGamme.Value_Intitule);
                                results.Append("|");
                                results.Append(doubleGamme.Intitule2);
                                results.Append("|");
                                results.Append(doubleGamme.Value_Intitule2);
                                return results;
                            }
                        }
                        else
                        {
                            if (doubleGamme.Reference.Equals(sku))
                            {
                                results.Append(article.Reference);
                                results.Append("|");
                                results.Append(doubleGamme.Intitule);
                                results.Append("|");
                                results.Append(doubleGamme.Value_Intitule);
                                return results;
                            }
                        }
                    }
                }
            }
            return results;
        }
        public static bool HasMethod(this object objectToCheck, string methodName)
        {
            var type = objectToCheck.GetType();
            return type.GetMethod(methodName) != null;
        }
    }
    
}
