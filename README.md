# xmigrator

I love Xamarin... but as much as I love it, I hate its Android designer plugin for both Xamarin & Visual Studio so I write all my UI using Android Studio (because the designer is AWESOME), therefore I *invented* this tool that helps me to *translate* all the resources inside the `res` folder to the Xamarin.Android's `Resource`

## Translation  
Yes, you are right: there is no need for such translation, since you can copy all the files directly and have them working right away without major modifications. But still, I miss my pascal casing!  and you know all resources within X.Android are written using pascal casing so this was a matter of consistency too.

## Usage

Simple:  
```
xmigrator [source] [target]
```  
`source` must be:  
 - An Android `res` folder: the tool will take whatever exists inside this folder and translate it into something more Xamarin friendly, placing it in `target`
 - A Xamarin.Android `Resource` folder: the tool will take whatever exists inside this folder and translate it into something Android Studio friendly, placing it in `target`
