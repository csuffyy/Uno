#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.UserDataTasks
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum UserDataTaskStoreAccessType 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AppTasksReadWrite,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AllTasksLimitedReadWrite,
		#endif
	}
	#endif
}
