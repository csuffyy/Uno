#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Security.Cryptography.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum Capi1KdfTargetAlgorithm 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NotAes,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Aes,
		#endif
	}
	#endif
}
