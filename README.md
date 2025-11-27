This is a prototype for emulation of the Prime Performer PT200 terminal, so there are quite a few bugs and quirks in it. But it's working! ;)
It uses Windows Forms as user interface, tried both console and WPF but WinForms turned out to be simpler and yet versatile enough. The modules used are located in the PT200Emulator_ConsoleTest repository.

The original was available with either green (most common) or amber phosphor, later a color version was introduced and also PC card that could be used together with the Prime PC (PC XT) - neither of them sold very much.
This emulator has options for green, amber, white and full color (have not tested that) and also a "blue" variant that is similar to the color used on HP terminals. You can also choose screen format to the four variants that was availbale.
Also in the config menu (entered with shift-ctrl-S) are the possibility to choose logging level, and if one turns on the debug flag in the config json you also have som more options (full or partial redraw, diag string overlay and Reconnect) and the logging will probably go there too later on.
The buttons Connect and Disconnect does exactly what one expects, but the actual function has not been tested fully yet as the Prime 50-series emulator has its own quirks.

It's been tested with both Primos and Emacs, and for most parts works as expected. There might still be some bugs regarding the actual keys sent and also with the backspace handling - but as for now it works acceptable.
