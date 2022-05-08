# BlazorClippy
Our hero...Mr. Clippy... is back to help us in the Blazor :)  The simple wrapper for the clippyjs package.

I would like to thanks to [Clippyjs](https://github.com/pi0/clippyjs) project. Next thanks goes to my old colleague Ondrej Vicar, who found the clippyjs before few years for me and created simple wrapper in electron in that days. I still was waiting when I will get back to the Clippy :D Thanks a lot Ondro :)

## Supported framework

The library and demo is built with .NET 6.0. Project was created in Microsoft Visual Studio 2022 (17.2.0 Preview 4).

## Basic usage

### Demo project

Please explore the [demo project](https://github.com/fyziktom/BlazorClippy/tree/main/src/BlazorClippy.Demo).

![image](https://user-images.githubusercontent.com/78320021/167314603-f19179e1-f459-4c42-afec-4d588702a3d6.png)

### Create Blazor Application and load library

Create Blazor empty project in Visual Studio

Add BlazorClippy nuget package


```code
dotnet add package BlazorClippy
```

### Add dependencies

Add this to the header

```html
<link rel="stylesheet" type="text/css" href="_content/BlazorClippy/clippy.css" media="all">
```


Add this to the body behind the <div id="app>Loading...</div>

```html
    <script src="_content/BlazorClippy/jquery.slim.min.js"></script>
    <script src="_content/BlazorClippy/clippy.min.js"></script>
    <script src="_content/BlazorClippy/clippyInterop.js"></script>
```

Add using and BlazorClippy service in the Program.cs
  
```csharp
  using BlazorClippy;
 
  .....
  
  builder.Services.AddScoped<ClippyService>();
```

### Load clippy
  
At start you need to initialized the Clippy. You can do it with call of clippy.Load() function. You can call this function during the load of the app also. But call it just once!
  
```html
@inject ClippyService clippy
  
<div class="row">
    <div class="col">
        <button class="btn btn-primary" onclick="@(async () => await clippy.Load())">Load</button>
    </div>
</div>
```
 

### Call other functions you need

```csharp
// play random animation
await clippy.AnimateRandom();

// Play specific animation - check ClippyAnimations enum to know all
await clippy.PlayAnimation(ClippyAnimations.GoodBye);

// Display text
await clippy.Speak("Hello All Blazor Lovers :)");

// Show gesture in some direction
await clippy.GestureAt(50,50);
```

More commands you can find in the [ClippyService](https://github.com/fyziktom/BlazorClippy/blob/main/src/BlazorClippy/ClippyService.cs).



