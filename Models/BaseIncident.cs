using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using FivePD.API.Utils;
using OfficerInvestigation.Enums;
using OfficerInvestigation.Interfaces;
using RemadeServices2._0;

namespace OfficerInvestigation.Models;

public class BaseIncident<IncidentType>() : IBaseIncident
{
    public static Dictionary<BaseIncident<IncidentType>, IncidentStatus> IncidentStatusMap = new();

    private IncidentStatus Status = IncidentStatus.Dispatched;
    
    private Guid serviceId = Guid.NewGuid();

    private List<Entity> RequestedEntities = [];

    public List<Entity> EntitiesToTrack = [];

    public Vector3 IncidentPosition;

    public float StartDistance = 20f;
    
    public Guid ServiceId => ServiceId;

    public virtual async Task OnTrigger()
    {
        Update();
        Utils.Print("OnTrigger");
        Status = IncidentStatus.Enroute;
        while (EntitiesToTrack.Count > 0 && EntitiesToTrack.All(e => e.Position.DistanceTo(IncidentPosition) > StartDistance))
            await BaseScript.Delay(100);
    }

    private async void Update()
    {
        IncidentStatusMap.Add(this, Status);
        while (Status != IncidentStatus.Completed)
        {
            IncidentStatusMap[this] = Status;
            await BaseScript.Delay(1000);
        }

        IncidentStatusMap.Remove(this);
    }

    public virtual Task OnArrival()
    {
        Utils.Print("OnArrival");
        Status = IncidentStatus.Arrived;
        throw new System.NotImplementedException();
    }

    public virtual Task OnFinish()
    {
        Utils.Print("OnFinish");
        Status = IncidentStatus.Completed;
        throw new System.NotImplementedException();
    }

    public virtual Task OnCancelBefore()
    {
        Utils.Print("OnCancelBefore");
        throw new System.NotImplementedException();
    }
    
    public async Task<Vehicle> SpawnServiceVehicle(VehicleHash vehicleHash)
    {
        Vehicle vehicle =
            await World.CreateVehicle(new(vehicleHash),
                Game.PlayerPed.Position.Around(100f).ClosestParkedCarPlacement());
        Utils.CaptureEntity(vehicle);
        Utils.EntitiesInMemory.Add(vehicle);
        RequestedEntities.Add(vehicle);
        return vehicle;
    }

    public async Task<Ped> SpawnServicePed(PedHash pedHash, Vehicle vehicle, VehicleSeat seat)
    {
        if (vehicle == null || !vehicle.Exists()) return null;
        Ped ped = await World.CreatePed(new(pedHash), vehicle.Position);
        ped.SetIntoVehicle(vehicle, seat);
        Utils.EntitiesInMemory.Add(ped);
        RequestedEntities.Add(ped);
        Utils.KeepTask(ped);
        /* JArray cache = (JArray)cachedServices[vehicle.Handle];
         cache[cache.Count] = ped.Handle;

         cachedServices[vehicle.Handle] = cache;*/
        return ped;
    }
}