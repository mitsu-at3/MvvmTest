using System;
using System.Collections.Generic;
using System.Text;

namespace MvvmTest.VM
{
	class MainModel : ViewModelBase, IMainModel
	{
		private Property<int> ans = new Property<int>();

		public int ParamA { get; set; }
		public int ParamB { get; set; }

		public int Answer
		{
			get => this.ans;
			set => this.ans.SetValue(value, this);
		}

		public void Sum()
			=> this.Answer = this.ParamA + this.ParamB;
	}
}
