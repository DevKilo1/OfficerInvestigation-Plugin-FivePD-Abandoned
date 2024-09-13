using System;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using FivePD.API;
using Kilo.Commons.Config;
using OfficerInvestigation.Enums;
using OfficerInvestigation.Incidents;
using OfficerInvestigation.Interfaces;
using OfficerInvestigation.Models;
using RemadeServices2._0;

namespace OfficerInvestigation
{
    public class InvestigationPlugin : Plugin
    {
        public Config Config;

        private Vehicle currentVehicle;
        private DateTime lastSeenWithVeh;
        private void StartupProcedure()
        {
            try
            {
                Config = new Config(AddonType.plugins, "{}", "OfficerInvestigations")
                {
                    ["Debug"] = true,
                };
                Updates();
                ListenForIncidents();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine("InvestigationPlugin errored during StartupProcedure! Contact developer.");
            } 
            Debug.WriteLine("Loaded Officer Investigations by DevKilo!");
        }
        
        internal InvestigationPlugin()
        {
            StartupProcedure();
        }

        private void Updates()
        {
            bool searchingForVehicle = false;
            Tick += async () =>
            {
                // UPDATES CURRENT VEHICLE VARIABLE.
                if (!searchingForVehicle && currentVehicle is null)
                {
                    Utils.Print("Searching for player's current vehicle...");
                    searchingForVehicle = true;
                    // Searching for new vehicle.
                    while (!Game.PlayerPed.IsSittingInVehicle() || Game.PlayerPed.IsSittingInVehicle() && API.NetworkGetEntityOwner(Game.PlayerPed.CurrentVehicle.Handle) != Game.Player.Handle)
                    {
                        await Delay(100);
                    }

                    currentVehicle = Game.PlayerPed.CurrentVehicle;
                    API.ClearEntityLastDamageEntity(currentVehicle.Handle);
                    lastSeenWithVeh = DateTime.Now;
                    Utils.Print("Registered player's current vehicle!");
                    searchingForVehicle = false;
                }

                if (currentVehicle is not null)
                {
                    if (Game.PlayerPed.CurrentVehicle == currentVehicle)
                    {
                        lastSeenWithVeh = DateTime.Now;                        
                    }

                    if (Game.PlayerPed.CurrentVehicle != currentVehicle)
                    {
                        if (DateTime.Now.Subtract(lastSeenWithVeh).Minutes > 10)
                        {
                            currentVehicle = null; // Initiates search for new current vehicle.
                        }
                    }
                }
                // 
            };
        }

        private async void Dispatch<IncidentType>() where IncidentType : BaseIncident<IncidentType>, new()
        {
            var s = new IncidentType();
            await s.OnTrigger();
            await s.OnArrival();
            await s.OnFinish();
            await s.OnCancelBefore();
        }

        private void ListenForIncidents()
        {
            bool creatingIncident = false;
            Tick += async () =>
            {
                if (currentVehicle is not null)
                {
                    if (currentVehicle.Driver is not null)
                    {
                        bool collisionActive =
                            BaseIncident<Collision>.IncidentStatusMap.Count > 0;
                        //Collision.IncidentStatusMap.ContainsKey()
                        if (!collisionActive && API.HasEntityBeenDamagedByAnyVehicle(currentVehicle.Handle))
                        {
                            if (creatingIncident) return;
                            creatingIncident = true;
                            Utils.Print("Collision detected");
                            Dispatch<Collision>();
                            await BaseScript.Delay(5000);
                            creatingIncident = false;
                        }
                    }
                }
                
            };
        }
    }
}