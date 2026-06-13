new GuiControlProfile (mmgTextProfile : GuiTextProfile)
{
	fontColor = "0 0 0";
	fontColorLink = "255 96 96";
	fontColorLinkHL = "0 0 255";
	fontColorNA = "128 128 128";
	autoSizeWidth = 1;
	autoSizeHeight = 1;
	fontOutlineColor = "0 0 0 64";
	doFontOutline = 1;
	fontOutlineWidth = 1;
};

new GuiControlProfile (MinimapWindowProfile : BlockWindowProfile)
{
	opaque = 1;
	border = 2;
	fillColor = "0 0 0 0";
	fillColorHL = "0 0 0 0";
	fillColorNA = "0 0 0 0";
	fillColor = "0 0 0 0";
	text = "";
	bitmap = "Add-Ons/Script_Minimap/tex/blankWindow";
	textOffset = "0 0";
	hasBitmapArray = 1;
	justify = "left";
};
	
new GuiCursor(mmgDragCursor : DefaultCursor)
{
	hotSpot = "12 12";
	bitmapName = "Add-Ons/Script_Minimap/tex/cur_drag";
};

// new GuiControlProfile(mmgBarProfile)
// {
	// opaque = 0;
	// fillColor = "0 204 0 255";
	// border = 1;
	// borderColor = "0 0 0";
// };