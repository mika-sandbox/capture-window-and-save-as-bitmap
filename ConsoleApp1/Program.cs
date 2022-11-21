// ------------------------------------------------------------------------------------------
//  Copyright (c) Natsuneko. All rights reserved.
//  Licensed under the MIT License. See LICENSE in the project root for license information.
// ------------------------------------------------------------------------------------------

using System.Drawing;
using System.Drawing.Imaging;

using ConsoleApp1;

using SharpGen.Runtime;

using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

// there is also a way to get hWnd directly from the Process ID, but in this case there are cases where an invisible windows is obtained, in which case DwmSharedSurface returns nullptr (IntPtr.Zero).
IntPtr GetWindowHandleFromProcessId(int id)
{
    var result = IntPtr.Zero;

    NativeMethods.EnumWindows((hWnd, lParam) =>
    {
        NativeMethods.GetWindowThreadProcessId(hWnd, out var processId);
        if (processId == id && NativeMethods.IsWindowVisible(hWnd))
        {
            result = hWnd;
            return true;
        }

        return true;
    }, IntPtr.Zero);

    return result;
}

Console.WriteLine("ScreenCapture | Save as PNG Format");


var featureLevels = new[]
{
    FeatureLevel.Level_11_0,
    FeatureLevel.Level_10_1,
    FeatureLevel.Level_10_0,
    FeatureLevel.Level_9_3,
    FeatureLevel.Level_9_2,
    FeatureLevel.Level_9_1
};

// create a simple device with primary gpu
using var factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();
factory.EnumAdapters(0, out var adapter);

// if you are not render texture into Window, you are not need SwapChain.
D3D11.D3D11CreateDevice(adapter, DriverType.Unknown, DeviceCreationFlags.None, featureLevels, out var device);

if (device == null)
{
    Console.WriteLine("ERR: failed to create a new D3D11Device");
    return;
}

Console.Write("Please input a process id to capture window: ");
var input = Console.ReadLine();
if (!int.TryParse(input, out var i))
{
    Console.WriteLine("ERR: Window Handle must be integer");
    return;
}

// get hWnd from Process ID
// if you can get hWnd directly, you are not need to this method call.
// DwmGetDxSharedSurface needs to provide hWnd, you can pass it directly.
var hWnd = GetWindowHandleFromProcessId(i);
if (hWnd == IntPtr.Zero)
{
    Console.WriteLine("ERR: failed to get hWnd from process id");
    return;
}

// get window surface
NativeMethods.DwmGetDxSharedSurface(hWnd, out var phSurface, out _, out _, out _, out _);

if (phSurface == IntPtr.Zero)
{
    Console.WriteLine("ERR: failed to get SharedSurface");
    return;
}

// copy window surface to our texture
var sharedSurface = device.OpenSharedResource<ID3D11Texture2D>(phSurface);
var description = new Texture2DDescription
{
    ArraySize = 1,
    BindFlags = BindFlags.None,
    CPUAccessFlags = CpuAccessFlags.Read,
    Format = Format.B8G8R8A8_UNorm,
    Height = sharedSurface.Description.Height,
    MipLevels = 1,
    SampleDescription = new SampleDescription(1, 0),
    Usage = ResourceUsage.Staging,
    Width = sharedSurface.Description.Width
};
var texture = device.CreateTexture2D(description);
device.ImmediateContext.CopyResource(texture, sharedSurface);

// save texture as bitmap
var box = device.ImmediateContext.Map(texture, 0);
using var bitmap = new Bitmap(description.Width, description.Height, PixelFormat.Format32bppArgb);
var dest = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
var copyBytes = bitmap.Width * 4; // color is RGBA format, it requires 4 bytes per pixel

// copy texture memory data to bitmap (this is the same as memcpy in C, running at low-level layer).
for (var y = 0; y < bitmap.Height; y++)
    MemoryHelpers.CopyMemory(dest.Scan0 + y * dest.Stride, box.DataPointer + y * box.RowPitch, copyBytes);

bitmap.UnlockBits(dest);
device.ImmediateContext.Unmap(texture, 0);

Console.Write("write texture to... (png) :");
var output = Console.ReadLine();
var basename = Path.GetDirectoryName(output);

if (!Directory.Exists(basename) || string.IsNullOrWhiteSpace(output))
{
    Console.WriteLine("ERR: directory not found");
    return;
}

bitmap.Save(output, ImageFormat.Png);

Console.WriteLine("write screen as texture is successful");