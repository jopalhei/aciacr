using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.ContainerInstance;
using Azure.ResourceManager.ContainerInstance.Models;
using Azure.ResourceManager.Models;
using Azure.ResourceManager.Resources;
using System;
using System.Net;
using System.Net.Sockets;

namespace ACITrigger
{
    public class MyContainer
    {
        public MyContainer() { }

        public string identityId = "/subscriptions/c2acf93f-1807-449c-a086-314038ce9f2c/resourcegroups/lab/providers/Microsoft.ManagedIdentity/userAssignedIdentities/myACRId";
        TokenCredential credential = new DefaultAzureCredential(); // Initialize with DefaultAzureCredential

        public void CreateContainerAsync()
        {
            Console.WriteLine("STARTED...");

            try
            {
                // Use DefaultAzureCredential, suitable for Azure VM with Managed Identity
                Console.WriteLine("Using DefaultAzureCredential");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                // The credential is already initialized to DefaultAzureCredential
                Console.WriteLine("Failed to acquire default credential in catch block");
            }

            string subnet = "/subscriptions/c2acf93f-1807-449c-a086-314038ce9f2c/resourceGroups/lab/providers/Microsoft.Network/virtualNetworks/lab-vnet/subnets/aci";
            string containerInstanceName = "acitest";
            string containerImage = "jopalheiacr.azurecr.io/samples/aci-helloworld";
            string containerRegistryServer = "jopalheiacr.azurecr.io";
            string resourceGroupName = "lab";
            string newContainerGroupName = containerInstanceName;
            //string containerRegistryUsername = "acrlopes";
            //string containerRegistryPassword = "password";

            var identity = new ManagedServiceIdentity(ManagedServiceIdentityType.UserAssigned);
            identity.UserAssignedIdentities.Add(new Azure.Core.ResourceIdentifier(identityId), new UserAssignedIdentity());
            Console.WriteLine("got identity");

            ArmClient client = new ArmClient(credential);
            var subscriptionId = "c2acf93f-1807-449c-a086-314038ce9f2c";
            var subscription = client.GetSubscriptionResource(new Azure.Core.ResourceIdentifier($"/subscriptions/{subscriptionId}"));
            ResourceGroupResource resourceGroup = subscription.GetResourceGroup(resourceGroupName).Value;
            var collection = resourceGroup.GetContainerGroups();
            Console.WriteLine("got collection");

            var containerInstance = new ContainerInstanceContainer(containerInstanceName, containerImage,
                new ContainerResourceRequirements(
              new ContainerResourceRequestsContent(4, 4)));

            ContainerInstanceOperatingSystemType osType = "Linux";
            var data = osType == ContainerInstanceOperatingSystemType.Linux
                ? new ContainerGroupData(
                    resourceGroup.Data.Location.Name,
                    new ContainerInstanceContainer[]
                    {
                        containerInstance
                    },
                    osType)
                {
                    RestartPolicy = ContainerGroupRestartPolicy.Never
                }
                : new ContainerGroupData(
                    resourceGroup.Data.Location.Name,
                    new ContainerInstanceContainer[]
                    {
                        containerInstance
                    },
                    osType)
                {
                    RestartPolicy = ContainerGroupRestartPolicy.Never,
                    Identity = identity
                };

            data.Identity = identity;
            data.ImageRegistryCredentials.Add(new ContainerGroupImageRegistryCredential(containerRegistryServer) { Identity = identityId });
            data.SubnetIds.Add(new ContainerGroupSubnetId(new Azure.Core.ResourceIdentifier(subnet)));
            data.Tags.Add("test1", "Test");
            data.Tags.Add("test2", "to-delete");
            Console.WriteLine("start creating...");

            try
            {
                ArmOperation<ContainerGroupResource> armOperation = collection.CreateOrUpdate(Azure.WaitUntil.Started, newContainerGroupName, data);
                Console.WriteLine("Created");
                Console.WriteLine(containerInstanceName);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                Console.WriteLine("FINISHED...");
            }
        }

        public void CheckResolveDNS(string url)
        {
            try
            {
                IPHostEntry hostInfo = Dns.GetHostEntry(url); // Updated method
                IPAddress[] address = hostInfo.AddressList;
                String[] alias = hostInfo.Aliases;
                Console.WriteLine("Host name : " + hostInfo.HostName);
                Console.WriteLine("\nAliases : ");
                for (int index = 0; index < alias.Length; index++)
                {
                    Console.WriteLine(alias[index]);
                }
                Console.WriteLine("\nIP Address list :");
                for (int index = 0; index < address.Length; index++)
                {
                    Console.WriteLine(address[index]);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException caught!!!");
                Console.WriteLine("Source : " + e.Source);
                Console.WriteLine("Message : " + e.Message);
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException caught!!!");
                Console.WriteLine("Source : " + e.Source);
                Console.WriteLine("Message : " + e.Message);
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine("NullReferenceException caught!!!");
                Console.WriteLine("Source : " + e.Source);
                Console.WriteLine("Message : " + e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception caught!!!");
                Console.WriteLine("Source : " + e.Source);
                Console.WriteLine("Message : " + e.Message);
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            MyContainer container = new MyContainer();
            container.CreateContainerAsync();
            container.CheckResolveDNS("example.com"); // Example usage
        }
    }
}
