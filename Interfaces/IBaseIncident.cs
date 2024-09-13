using System.Threading.Tasks;
using OfficerInvestigation.Enums;

namespace OfficerInvestigation.Interfaces;

public interface IBaseIncident
{
    Task OnTrigger();
    Task OnArrival();
    Task OnFinish();
    Task OnCancelBefore();
}