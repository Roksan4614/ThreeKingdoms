mergeInto(LibraryManager.library,
{	
	OpenURL: function(url) 
	{	
	  const mobileType = navigator.userAgent.toLowerCase(); 
	  if (mobileType.indexOf('iphone') > -1 || mobileType.indexOf('ipad') > -1 || mobileType.indexOf('ipod') > -1) {
		url            = UTF8ToString(url);		
		document.documentElement.addEventListener('pointerup', function () {
				window.open(url);
		}, { once: true });
	  } else {
		var link = document.createElement('a');
		link.id = 'link';
		link.setAttribute('href', UTF8ToString(url));
		link.setAttribute('target', '_blank');
		document.body.appendChild(link);
		link.click();
	  }	  
	},
		
	GetBrowserType: function() 
	{
		const agent = window.navigator.userAgent.toLowerCase();
		var bName;
		
		switch(true)
		{
			case agent.indexOf("edg/") > -1:
				bName = "edge";
				break;
				
			case agent.indexOf("opr") > -1 && !!window.opr:
				bName = "opr";
				break;
				
			case agent.indexOf("chrome") > -1 && !!window.chrome:
				bName = "chrome";
				break;
			
			case agent.indexOf("firefox") > -1:
				bName = "firefox";
				break;
				
			case agent.indexOf("safari") > -1:
				bName = "safari";
				break;
			
			default:
				bName = "other";
				break;
		}
		
		SendMessage("SceneScript", "SetBrowserType", bName);
	},	
		
	FirebaseRequestAuth: function(type, id, password)
	{
		UnityHandler_FirebaseRequestAuth(type, UTF8ToString(id), UTF8ToString(password));
	},

	FirebaseRefreshToken: function()
	{
		UnityHandler_FirebaseRefreshToken();
	},

	FirebaseSignOut: function()
	{
		UnityHandler_FirebaseSignOut();
	},

	FirebaseLogEvent: function(_key, _value)
	{
		UnityHandler_FirebaseLogEvent(_key, _value);
	},
	
	StartGame: function()
	{
		UnityHandler_StartKingzTongits();
		HideCurtain();
	},

	CopyText: function(_text){
		navigator.clipboard.writeText(UTF8ToString(_text));
	},

	FullScreen: function(_isFullScreen){
		UnityHandler_FullScreen(_isFullScreen);
	},
	
	UnityProgressCall : function(index, per)
	{
		UnityProgressCall(index, per);
	}
});





