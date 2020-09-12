using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;

namespace MvvmTest.VM
{
	public class MainViewModel : ViewModelBase
	{
		private IMainModel Model { get; }

		private Property<string> paramA = new Property<string>("0",
			(value) =>
			{
				if (value.Length == 0)
					return "未入力エラー";
				else if (!int.TryParse(value, out int _))
					return "フォーマットエラー";
				else
					return null;
			});

		private Property<string> paramB = new Property<string>("0",
			(value) =>
			{
				if (value.Length == 0)
					return "未入力エラー";
				else if (!int.TryParse(value, out int _))
					return "フォーマットエラー";
				else
					return null;
			});

		private Property<int> ans = new Property<int>();

		public string ParamA
		{
			get => this.paramA;
			set
			{
				// 入力エラーがなければModelに設定
				if (this.paramA.SetValue(value, this))
					this.Model.ParamA = int.Parse(this.ParamA);
			}
		}

		public string ParamB
		{
			get => this.paramB;
			set
			{
				// 入力エラーがなければModelに設定
				if (this.paramB.SetValue(value, this))
					this.Model.ParamB = int.Parse(this.ParamB);
			}
		}

		public int Answer
		{
			get => this.ans;
			set => this.ans.SetValue(value, this);
		}

		public ICommand SumCommand { get; }

		public MainViewModel()
		{
			// TODO: 本来ならDIコンテナから取得
			this.Model = new MainModel();
			this.Model.PropertyChanged += Model_PropertyChanged;

			// Sumボタン
			this.SumCommand = new Command(() =>
			{
				// 実行
				this.Model.Sum();
			}, paramA, paramB);
		}

		private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(this.Model.Answer))
				this.Answer = this.Model.Answer;
		}
	}

	public class ViewModelBase : INotifyPropertyChanged, INotifyDataErrorInfo
	{
		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			this.PropertyChanged?.Invoke(this, e);
		}

		#endregion

		#region INotifyDataErrorInfo

		public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

		private Dictionary<string, IEnumerable> Errors { get; }
			= new Dictionary<string, IEnumerable>();

		public bool HasErrors => this.Errors.Any();

		public IEnumerable GetErrors(string propertyName)
		{
			if (string.IsNullOrEmpty(propertyName))
				return null;
			else if (this.Errors.TryGetValue(propertyName, out IEnumerable errors))
				return errors;
			else
				return null;
		}

		protected virtual void OnErrorsChanged(DataErrorsChangedEventArgs e)
		{
			this.ErrorsChanged?.Invoke(this, e);
		}

		private bool NotifyValidate(string propertyName, IEnumerable errors)
		{
			if (errors != null)
			{
				// エラーが既に存在して変化がない場合は何もしない
				if (this.Errors.TryGetValue(propertyName, out IEnumerable oldErrors) &&
					object.Equals(oldErrors, errors)) return false;

				this.Errors[propertyName] = errors;
				this.OnErrorsChanged(
					new DataErrorsChangedEventArgs(propertyName));

				return true;
			}
			else if (this.Errors.Remove(propertyName))
			{
				this.OnErrorsChanged(
					new DataErrorsChangedEventArgs(propertyName));
				return true;
			}

			return false;
		}

		#endregion

		protected class Command : ICommand
		{
			private IProperty[] Properties { get; }

			private Action executeAction;

			public Command(Action execute, params IProperty[] properties)
			{
				this.executeAction = execute;
				this.Properties = properties;
				
				foreach (var prop in properties)
					prop.RegisterCommand(this);
			}

			~Command()
			{
				foreach (var prop in this.Properties)
					prop.UnregisterCommand(this);
			}

			public event EventHandler CanExecuteChanged;

			public bool CanExecute(object parameter)
			{
				return this.Properties.All((prop) => !prop.HasErrors);
			}

			public void Execute(object parameter)
			{
				this.executeAction();
			}

			protected internal void OnCanExecuteChanged(EventArgs e)
			{
				this.CanExecuteChanged?.Invoke(this, e);
			}
		}

		protected interface IProperty
		{
			IEnumerable Errors { get; }
			bool HasErrors { get; }
			void RegisterCommand(Command cmd);
			void UnregisterCommand(Command cmd);
		}

		protected class Property<T> : IProperty
		{
			public T Value { get; private set; }
			public Func<T, IEnumerable> ValidateFunction { get; set; }

			public IEnumerable Errors { get; private set; }
			public bool HasErrors => this.Errors != null;

			private List<Command> Commands { get; } = new List<Command>();

			public Property() : this(default(T)) { }

			public Property(T defaultValue) : this(defaultValue, (_) => null) { }

			public Property(Func<T, IEnumerable> validateFunc) : this(default, validateFunc) { }

			public Property(T defaultValue, Func<T, IEnumerable> validateFunc)
			{
				this.Value = defaultValue;
				this.ValidateFunction = validateFunc;
			}

			public bool SetValue(T value,
				ViewModelBase source,
				[CallerMemberName] string propName = null)
			{
				if (!object.Equals(value, this.Value))
				{
					this.Value = value;

					source.OnPropertyChanged(
						new PropertyChangedEventArgs(propName));

					this.Errors = this.ValidateFunction(this.Value);

					if (source.NotifyValidate(propName, this.Errors))
					{
						foreach (var cmd in Commands)
							cmd.OnCanExecuteChanged(EventArgs.Empty);
					}
				}

				return !this.HasErrors;
			}

			void IProperty.RegisterCommand(Command cmd)
				=> this.Commands.Add(cmd);

			void IProperty.UnregisterCommand(Command cmd)
				=> this.Commands.Remove(cmd);

			public static implicit operator T(Property<T> prop)
			{
				return prop.Value;
			}
		}
	}
}
