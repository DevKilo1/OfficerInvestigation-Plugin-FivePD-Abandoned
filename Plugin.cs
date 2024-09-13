using CitizenFX.Core;
using FivePD.API;

namespace OfficerInvestigation
{
    public class InvestigationPlugin: Plugin
    {
        internal InvestigationPlugin()
        {
            StartupProcedure();
        }

        private void StartupProcedure()
        {
            Debug.WriteLine("Loaded Officer Investigations by DevKilo!");
        }
    }
}