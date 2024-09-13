using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using FivePD.API.Utils;
using OfficerInvestigation.Enums;
using OfficerInvestigation.Models;
using RemadeServices2._0;

namespace OfficerInvestigation.Incidents;

public class Collision() : BaseIncident<Collision>()
{
    
    public override async Task OnTrigger()
    {
        IncidentPosition = Game.PlayerPed.Position;
        // Capture Victims
        var vehiclesInvolved = World.GetAllVehicles()
            .Where(v =>
                v is not null && v.Position.DistanceTo(IncidentPosition) < 50f &&
                API.HasEntityBeenDamagedByEntity(Game.PlayerPed.CurrentVehicle.Handle, v.Handle, true)).ToList();
        List<Ped> pedsInvolved = new List<Ped>();
        
        foreach (var v in vehiclesInvolved)
        {
            Utils.CaptureEntity(v);
            new Action(async () =>
            {
                var veh = await SpawnServiceVehicle((VehicleHash)v.Model.Hash);
                foreach (var p in v.Occupants)
                {
                    Utils.CaptureEntity(p);
                    var clone = p.Clone();
                    var seat = (VehicleSeat)p.SeatIndex;
                    clone.SetIntoVehicle(veh, seat);
                    clone.Health = p.Health;
                    pedsInvolved.Add(clone);
                    p.Delete();
                }

                API.CopyVehicleDamages(v.Handle, veh.Handle);
                veh.Mods.Livery = v.Mods.Livery;
                veh.Mods.ColorCombination = v.Mods.ColorCombination;
                Vector3 pos = v.Position;
                float heading = v.Heading;
                v.Delete();
                veh.Position = pos;
                veh.Heading = heading;
                var m = new Utils.Marker(MarkerType.ThickChevronUp, Utils.MarkerAttachTo.Entity, veh.Position, veh);
                m.SetOffset(new(0f, 0f, 2f));
                m.SetMovement(true, true);
                m.SetColor(255,0,0);
                m.SetRotation(new(0f, 180f,0f));
                veh.IsEngineRunning = true;
                var parkPos = await Utils.HandlePark(veh);
                IncidentPosition = (Vector3)parkPos;
            })();
        }
        // Spawn AI

        var supervisorVeh = await this.SpawnServiceVehicle(VehicleHash.Police);
        supervisorVeh.IsEngineRunning = true;
        var supervisor = await this.SpawnServicePed(PedHash.Cop01SMY, supervisorVeh, VehicleSeat.Driver);
        supervisor.AttachBlip();
        var supe = new Utils.Waypoint(IncidentPosition, supervisorVeh);
        supe.SetDrivingSpeed(10f);
        //supervisor.Task.DriveTo(supervisorVeh, IncidentPosition, supervisorVeh.Model.GetDimensions().Length() / 2, 20f, supe.DrivingStyle);
        Utils.SlowVehicleDownInRadiusToPosition(supervisorVeh, IncidentPosition.ClosestParkedCarPlacement(), 10f, supe.DrivingStyle);
        supe.Start();
        supervisorVeh.IsSirenActive = true;
        supervisorVeh.IsSirenSilent = true;
        await supe.Wait();
        
        this.EntitiesToTrack.Add(supervisor);
        EntitiesToTrack.Add(supervisorVeh);
        
        await base.OnTrigger();
    }
}