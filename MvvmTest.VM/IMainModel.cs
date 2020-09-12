using System.ComponentModel;

namespace MvvmTest.VM
{
	public interface IMainModel : INotifyPropertyChanged, INotifyDataErrorInfo
	{
		int ParamA { get; set; }
		int ParamB { get; set; }
		int Answer { get; }
		void Sum();
	}
}
