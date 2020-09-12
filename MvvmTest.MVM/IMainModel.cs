using System.ComponentModel;

namespace MvvmTest.MVM
{
	public interface IMainModel : INotifyPropertyChanged, INotifyDataErrorInfo
	{
		int ParamA { get; set; }
		int ParamB { get; set; }
		int Answer { get; }
		void Sum();
	}
}
