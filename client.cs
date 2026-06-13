// i weep

//todo: resize large map border with resolution
//todo: arg for non-important icons that disappear off the edge rather than staying in the radar
//todo: make dragging use movemap to hold rather than toggle

if(!isObject("mmgTextProfile"))
{
	exec("./profile.cs");
}

function rgb2hex(%rgb)
{
	%r = comp2hex(255 * getWord(%rgb, 0));
	%g = comp2hex(255 * getWord(%rgb, 1));
	%b = comp2hex(255 * getWord(%rgb, 2));
 
	return %r @ %g @ %b;
}

function comp2hex(%comp)
{
	%left = mFloor(%comp / 16);
	%comp = mFloor(%comp - %left * 16);
	
	%left = getSubStr("0123456789ABCDEF", %left, 1);
	%comp = getSubStr("0123456789ABCDEF", %comp, 1);
	
	return %left @ %comp;
}

if(!isObject(MMGRadar))
	exec("./MinimapGui.gui");

if(!isObject(MMGSettingsDlg))
	exec("./MMGSettingsDlg.gui");

function isBlocklandRebuilt()
{
	return 0;
}

exec("./binds.cs");
exec("./ProjectionMatrix.cs"); // Courtesy of Hologlaxer

addKeyBind("Minimap", "Toggle Large Map", MMGToggleLargeMap, "keyboard", "f3", false);
addKeyBind("Minimap", "Show Large Map", MMGShowLargeMap, "", "", false);
addKeyBind("Minimap", "Open Settings", MMGOpenSettings, "keyboard", "ctrl f3", false);

if($Pref::Client::mmgRadarRadius $= "") $Pref::Client::mmgRadarRadius = 128;
if($Pref::Client::mmgDisplayIcons $= "") $Pref::Client::mmgDisplayIcons = true;
if($Pref::Client::mmgDisplayIconsTPV $= "") $Pref::Client::mmgDisplayIconsTPV = false;
if($Pref::Client::mmgDisplayIconsFL $= "") $Pref::Client::mmgDisplayIconsFL = false;
if($Pref::Client::mmgAlwaysNorth $= "") $Pref::Client::mmgAlwaysNorth = false;
if($Pref::Client::mmgSquare $= "") $Pref::Client::mmgSquare = false;
if($Pref::Client::mmgCompass $= "") $Pref::Client::mmgCompass = isBlocklandRebuilt();
if($Pref::Client::mmgCompassDot $= "") $Pref::Client::mmgCompassDot = !isBlocklandRebuilt();
if($Pref::Client::mmgFont $= "") $Pref::Client::mmgFont = "<font:Arial:12>";

function MMGReset()
{
	cancel(MMGRadar.loop);

	dumpSetTo(MMGRadarLargeContainer, MMGRadarContainer);

	MMGRadarLarge.setVisible(false);
	MMGRadar.setVisible(false);
	MMGRadarContainer.setVisible(false);
	MMGRadarContainer.deleteAll();
	MMGWorldIconContainer.deleteAll();
	MMGText.setVisible(false);
	MMGText.setValue("");

	Canvas.popDialog(MMGRadarLargeWindow);

	clientCmdMMGSetViewOffset("0 0");
	clientCmdMMGSetViewScale(1);
	clientCmdMMGSetLargeViewOffset("0 0");
	clientCmdMMGSetLargeViewScale(1);

	$mmgMapOffset = "0 0 0";
	$mmgStartDragPos = "0 0";
	$mmgMapDragging = 0;
	$mmgLargeZoom = 1;
	$mmgActive = false;
	$mmgHeadData = -1;
}

//+ getMin(128-$mmgRadarRadius, 0)
//1280-256-64-19 = 941
function MMGUpdateGui()
{
	$mmgRadarRadius = $Pref::Client::mmgRadarRadius;
	if($Pref::Client::HorizontalHUD)
	{
		MMGRadar.position = (Canvas.getExtent() - $Pref::Client::HorizontalHUDOffset - $mmgRadarRadius*2 - 12) SPC 12;
		MMGText.position = (Canvas.getExtent() - $Pref::Client::HorizontalHUDOffset - $mmgRadarRadius*2 - 12) SPC ($mmgRadarRadius*2 + 16);
	}
	else 
	{
		%hp = (HUD_EnergyBar.isVisible() ? 11 : 0);
		if(isObject("HUD_HealthBar"))
		{
			%hp = %hp + (HUD_HealthBar.isVisible() ? 16 : 0);
			%hp = %hp + (HUD_HealthBarVehicle.isVisible() ? 16 : 0);
		}
		MMGRadar.position = (Canvas.getExtent() - $Pref::Client::HorizontalHUDOffset - $mmgRadarRadius*2 - 83) SPC 12 + %hp;
		MMGText.position = (Canvas.getExtent() - $Pref::Client::HorizontalHUDOffset - $mmgRadarRadius*2 - 83) SPC ($mmgRadarRadius*2 + 16) + %hp;
	}
	MMGRadar.extent = $mmgRadarRadius*2 SPC $mmgRadarRadius*2;
	MMGText.extent = $mmgRadarRadius*2 SPC 14;
	MMGText.forceReflow();
	MMGRadarCompass.position = "0 0";
	MMGRadarContainer.position = vectorSub(MMGRadar.position, "30 30");
	MMGRadarContainer.extent = vectorAdd(MMGRadar.getExtent(), "60 60");
	if($Pref::Client::mmgSquare)
	{
		MMGRadar.setBitmap("add-ons/script_minimap/tex/bg_square");
	} else {
		if($mmgRadarRadius <= 96) //small images since downscaling looks bad
		{
			MMGRadar.setBitmap("add-ons/script_minimap/tex/bg_radarTiny");
		} else if($mmgRadarRadius <= 160)
		{
			MMGRadar.setBitmap("add-ons/script_minimap/tex/bg_radarSmall");
		} else {
			MMGRadar.setBitmap("add-ons/script_minimap/tex/bg_radar");
		}
	}
	
	if($Pref::Client::mmgAlwaysNorth)
	{
		MMGRadarCompass.setBitmap("Add-Ons/Script_Minimap/tex/compass2");
		MMGRadarCompassDot.setBitmap("Add-Ons/Script_Minimap/tex/compass_dot");
	}
	else {
		MMGRadarCompass.setBitmap("Add-Ons/Script_Minimap/tex/compass");
		MMGRadarCompassDot.setBitmap("Add-Ons/Script_Minimap/tex/compass_dot2");
	}
	
	MMGRadar.viewScale = ($MMGRadarScale * (128 / $Pref::Client::mmgRadarRadius));
	MMGRadarCompass.setVisible($Pref::Client::mmgCompass);
	MMGRadarCompassDot.setVisible($Pref::Client::mmgCompassDot);
	
	MMGRadarLargeWindow.extent = Canvas.getExtent();
	MMGRadarLargeWindow.position = "0 0";
	
	MMGWorldIconContainer.extent = Canvas.getExtent();
	MMGWorldIconContainer.position = "0 0";
	
	MMGRadarLarge.extent = vectorSub(Canvas.getExtent(), "240 200");
	MMGRadarLarge.position = "120 100";

	MMGRadarLargeContainer.extent = vectorSub(Canvas.getExtent(), "244 204");
	MMGRadarLargeContainer.position = "2 2";
	
	if(!MMGRadarLarge.isVisible())
	{
		MMGRadarCompass.extent = MMGRadar.getExtent();
	} else {
		MMGRadarCompass.extent = MMGRadarLarge.getExtent();
	}
}

function MMGRadarClicked(%rightClick)
{
	if($mmgMapDragging)
	{
		MMGRadarSpacebar();
		return;
	}
	
	%pos = Canvas.getCursorPos();

	%pt = vectorSub(%pos, MMGRadarLargeOverlay.getScreenPosition());
	%obj = ServerConnection.getControlObject();
	%form = mmgGetCamera(%obj);
	%pos = getWords(%form, 0, 2);
	if($Pref::Client::mmgAlwaysNorth)
	{
		%fwd = "0 1 0";
	} else {
		%fwd = getWords(%form, 3, 5);
	}
	%pos = vectorAdd(%pos, $mmgMapOffset);

	%p = mmgVector(vectorSub(vectorScale(MMGRadarLargeOverlay.getExtent(), 0.5), %pt), %fwd);
	%p = vectorScale(%p, $mmgLargeZoom * MMGRadar.largeViewScale);
	%p = vectorAdd(setWord(%p, 0, -1 * getWord(%p, 0)), %pos);
	
	%ico = 0;
	%cts = MMGRadarLargeContainer.getCount();
	for(%i = 0; %i < %cts; %i++)
	{
		%o = MMGRadarLargeContainer.getObject(%i);

		if(%o.sourceId $= "")// && %o != MMGRadar.playerIcon)
			continue;

		if(vectorDist(%pt, vectorAdd(%o.getPosition(), vectorScale(%o.getExtent(), 0.5))) < $mmgIconSize / 1.5)
		{
			%ico = %o;
			break;
		}
	}

	if(!isObject(%ico))
		commandToServer('MMGMapClicked', getWords(%p, 0, 1), %rightClick);
	else
		commandToServer('MMGIconClicked', getWords(%p, 0, 1), %ico.sourceId, %rightClick);
}

function MMGRadarSpacebar(%click)
{
	if(%click $= "")
	{
		%click = !$mmgMapDragging;
	}
	%curPos = Canvas.getCursorPos();

	%pt = vectorSub(%curPos, MMGRadarLargeOverlay.getScreenPosition());
	%obj = ServerConnection.getControlObject();
	%form = mmgGetCamera(%obj);
	%pos = getWords(%form, 0, 2);
	
	if($Pref::Client::mmgAlwaysNorth)
	{
		%fwd = "0 1 0";
	} else {
		%fwd = getWords(%form, 3, 5);
	}

	%p = mmgVector(vectorSub(vectorScale(MMGRadarLargeOverlay.getExtent(), 0.5), %pt), %fwd);
	%p = vectorAdd(setWord(%p, 0, -1 * getWord(%p, 0)), %pos);
	%p = vectorScale(%p, $mmgLargeZoom);
	
	if(%click && !$mmgMapDragging)
	{
		$mmgStartDragPos = %p;
		$mmgMapDragging = 1;
		Canvas.setCursor(mmgDragCursor);
		MMGRadarResetBtn.setActive(1);
	} else if(!%click && $mmgMapDragging) {
		$mmgMapOffset = vectorAdd($mmgMapOffset, vectorSub($mmgStartDragPos,%p));
		$mmgMapDragging = 0;
		Canvas.setCursor(DefaultCursor);
		if(%p $= $mmgStartDragPos)
		{
			MMGRadarClicked(1);
		}
		MMGRadarResetBtn.setActive($mmgLargeZoom != 1 || $mmgMapOffset !$= "0 0 0");
	}
}

function MMGRadarLargeOverlay::onMouseUp(%this,%modifier,%mousePoint,%mouseClickCount)
{
	MMGRadarClicked(-1);
}

function MMGRadarLargeOverlay::onRightMouseDown(%this,%modifier,%mousePoint,%mouseClickCount)
{
	MMGRadarSpacebar(1);
}

function MMGRadarLargeOverlay::onRightMouseUp(%this,%modifier,%mousePoint,%mouseClickCount)
{
	MMGRadarSpacebar(0);
}

function MMGResetDrag()
{
	Canvas.setCursor(DefaultCursor);
	$mmgMapDragging = 0;
	$mmgMapOffset = "0 0 0";
	$mmgLargeZoom = 1;
	MMGRadarResetBtn.setActive(0);
	MMGRadarZoomInBtn.setActive(1);
	MMGRadarZoomOutBtn.setActive(1);
}

function MMGZoom(%in)
{
	if(%in $= "-1")
	{
		%amt = -0.2;
	} else {
		%amt = 0.25;
	}
	$mmgLargeZoom = mClampF($mmgLargeZoom * (1 + %amt), 0.107374, 3.05175);
	//talk($mmgLargeZoom);
	
	MMGRadarZoomInBtn.setActive(!($mmgLargeZoom $= "0.107374"));
	MMGRadarZoomOutBtn.setActive(!($mmgLargeZoom $= "3.05175"));
	MMGRadarResetBtn.setActive($mmgLargeZoom != 1 || $mmgMapOffset !$= "0 0 0");
}

package MinimapPkg
{
	function disconnectedCleanup(%bool) // todo?: clean up icons when changing maps too
	{
		MMGReset();
		$MMGEnabled = 0;

		parent::disconnectedCleanup(%bool);
	}
	
	function PlayGui::onRender(%this)
	{
		Parent::onRender(%this);
		MMGUpdateGui();
	}
	
	function optionsDlg::onSleep(%this)
	{
		parent::onSleep(%this);
		MMGUpdateGui();
	}
	
	function scrollInventory(%val)
	{
		if(MMGRadarLarge.isVisible())
		{
			MMGZoom(mClampF(%val*-1,-1,1));
		} else {
			parent::scrollInventory(%val);
		}
	}
	
	function clientCmdSetHealthBarVisible(%bool)
	{
		Parent::clientCmdSetHealthBarVisible(%bool);
		
		MMGUpdateGui();
	}
	
	function clientCmdSetHealthBarVehicleVisible(%bool)
	{
		Parent::clientCmdSetHealthBarVehicleVisible(%bool);
		
		MMGUpdateGui();
	}
	
	function clientCmdShowEnergyBar(%bool)
	{
		Parent::clientCmdShowEnergyBar(%bool);
		
		MMGUpdateGui();
	}
};
activatePackage(MinimapPkg);


$mmgIconSize = 16;

function dumpSetTo(%from, %to)
{
	while(%from.getCount() > 0)
	{
		%obj = %from.getObject(0);
		%from.remove(%obj);
		%to.add(%obj);
	}
}

function MMGToggleLargeMap(%val)
{
	if(!isObject(ServerConnection) || !isObject(ServerConnection.getControlObject()) || !$mmgActive)
		return;

	if(%val)
	{
		if(MMGRadarLarge.isVisible())
			MMGShowLargeMap(0);
		else
			MMGShowLargeMap(1);
	}
}

function MMGShowLargeMap(%val)
{
	if(!isObject(ServerConnection) || !isObject(ServerConnection.getControlObject()) || !$mmgActive)
		return;

	MMGResetDrag();
	if(%val)
	{
		MMGRadarLarge.add(MMGRadarCompass);
		MMGRadarLarge.add(MMGRadarCompassDot);
		MMGRadarCompass.extent = MMGRadarLarge.getExtent();
		MMGRadarCompass.position = "0 0";
		dumpSetTo(MMGRadarContainer, MMGRadarLargeContainer);
		MMGRadar.setVisible(false);
		MMGText.setVisible(false);
		MMGRadarLarge.setVisible(true);

		%pos = MMGRadarLarge.getPosition();
		%ext = MMGRadarLarge.getExtent();
		MMGRadarLargeOverlay.resize(getWord(%pos, 0) + 2, getWord(%pos, 1) + 2, getWord(%ext, 0) - 4, getWord(%ext, 1) - 4);
		MMGRadarCloseBtn.position = (getWord(%ext, 0) + getWord(%pos, 0) - 32) SPC getWord(%pos, 1) + 8;
		MMGRadarOptBtn.position = (getWord(%ext, 0) + getWord(%pos, 0) - 64) SPC getWord(%pos, 1) + 8;
		
		//MMGRadarResetBtn.position = (getWord(%ext, 0) + getWord(%pos, 0) - 96) SPC getWord(%pos, 1) + 8;
		//MMGRadarZoomOutBtn.position = (getWord(%ext, 0) + getWord(%pos, 0) - 32) SPC (getWord(%pos, 1) +  getWord(%ext, 1) - 32);
		//MMGRadarZoomInBtn.position = (getWord(%ext, 0) + getWord(%pos, 0) - 32) SPC (getWord(%pos, 1) +  getWord(%ext, 1) - 64);
		
		MMGRadarResetBtn.position = (getWord(%ext, 0)*0.5 + getWord(%pos, 0) - 12) SPC (getWord(%pos, 1) +  getWord(%ext, 1) - 32);
		MMGRadarZoomOutBtn.position = (getWord(%ext, 0)*0.5 + getWord(%pos, 0) - 44) SPC (getWord(%pos, 1) +  getWord(%ext, 1) - 32);
		MMGRadarZoomInBtn.position = (getWord(%ext, 0)*0.5 + getWord(%pos, 0) + 20) SPC (getWord(%pos, 1) +  getWord(%ext, 1) - 32);
		

		Canvas.pushDialog(MMGRadarLargeWindow);
		MMGRadarResetBtn.setActive(0);

		// cursorOn();
	}
	else
	{
		MMGRadar.add(MMGRadarCompass);
		MMGRadar.add(MMGRadarCompassDot);
		MMGRadarCompass.extent = MMGRadar.getExtent();
		MMGRadarCompass.position = "0 0";
		dumpSetTo(MMGRadarLargeContainer, MMGRadarContainer);
		MMGRadar.setVisible(true);
		MMGText.setVisible(true);
		MMGRadarLarge.setVisible(false);
		
		Canvas.popDialog(MMGRadarLargeWindow);
		$mmgMapDragging = 0;
		$mmgStartDragPos = "0 0";

		// cursorOff();
	}
}

function mmgGetCamera(%obj)
{
	if(!isObject(%obj))
		return "";

	%odb = %obj.getDataBlock();
	%up = %obj.getUpVector();
	%fwd = vectorNormalize(setWord(%obj.getForwardVector(), 2, 0));
	%right = vectorNormalize(vectorCross(%fwd, %up));
	%pos = getWords(%obj.getTransform(), 0, 2);

	if(getWord(%up, 2) < 0)
		%right = vectorScale(%right, -1);

	return %pos SPC %fwd SPC %up SPC %right;
}

// I have no idea how any of this vector math even works, but it does somehow

// I actually think this function is broken so probably don't use it
// It just happens to work fine for this mod
function mmgVector(%vec, %dir, %up)
{
	if(%up $= "")
		%up = "0 0 1";

	%dir = vectorNormalize(%dir);
	%up = vectorNormalize(%up);

	%right = vectorNormalize(vectorCross(%dir, %up));

	%dotX = vectorDot(%right, "1 0 0");
	%dotY = vectorDot(%dir, "0 1 0");

	%dotXX = vectorDot(%right, "0 1 0");
	%dotYY = vectorDot(%dir, "1 0 0");
	
	%dx = getWord(%vec, 0) * %dotX;
	%dy = getWord(%vec, 1) * %dotY;

	%dx = %dx + getWord(%vec, 1) * %dotXX;
	%dy = %dy + getWord(%vec, 0) * %dotYY;

	return %dx SPC %dy SPC getWord(%vec, 2);
}

function mmgUpdateIcon(%ico, %pos, %offPos, %fwd, %delta, %center, %size, %rad, %scale, %mapOffset)
{
	if(%ico.hide)
	{
		if(%ico.isVisible())
			%ico.setVisible(false);
		
		if(%ico.world.isVisible())
			%ico.world.setVisible(false);
	}
	else
	{
		if(!%ico.isVisible())
			%ico.setVisible(true);

		if(!%ico.world.isVisible())
			%ico.world.setVisible(true);
	}

	%p = vectorAdd(%ico.pos, vectorScale(%ico.vel, %delta));
	%ico.pos = %p;

	%drawPos = vectorAdd(%mapOffset, %pos);
	%p = vectorSub(%p, %offPos);
	%p = setWord(vectorSub(%p, %drawPos), 2, 0);
	%dir = vectorNormalize(%p);

	%dir = mmgVector(%dir, %fwd);
	%y = getWord(%dir, 1);
	%dir = setWord(%dir, 1, -1 * %y);

	%len = vectorLen(%p) / %scale;
	
	%iconSize = $mmgIconSize;
	if(MMGRadarLargeContainer.isVisible())
	{
		%iconSize = mClamp($mmgIconSize / $mmgLargeZoom, 8, 512);
	}
	
	
	if(%rad)
	{
		if(%len > %size)
			%len = %size;
	}

	if(%ico.showDist)
	{
		if(%ico.text.txt !$= "")
		{
			%ico.text.setText(%ico.text.base @ %ico.text.txt @ "<br>" @ mFloatLength(vectorDist(%ico.pos, %pos), 0) @ "u");
			%ico.worldText.setText(%ico.text.base @ %ico.text.txt @ "<br>" @ mFloatLength(vectorDist(%ico.pos, %pos), 0) @ "u");
		}
		else
		{
			%ico.text.setText(%ico.text.base @ mFloatLength(vectorDist(%ico.pos, %drawPos), 0) @ "u");
			%ico.worldText.setText(%ico.text.base @ mFloatLength(vectorDist(%ico.pos, %pos), 0) @ "u");
		}
	}

	if(%rad)
		%off = "30 30";
	else
		%off = "0 0";

	if(%rad)
		%upos = vectorAdd(vectorSub(vectorAdd(%center, vectorScale(%dir, %len)), vectorScale(%iconSize SPC %iconSize, 0.5)), %off);
	else
	{
		%p = vectorScale(%dir, %len);

		%px = getWord(%p, 0);
		%sx = getWord(%size, 0);
		%py = getWord(%p, 1);
		%sy = getWord(%size, 1);

		if(%px > %sx / 2)
			%px = %sx / 2;
		else if(%px < -(%sx / 2))
			%px = -(%sx / 2);

		if(%py > %sy / 2)
			%py = %sy / 2;
		else if(%py < -(%sy / 2))
			%py = -(%sy / 2);

		%p = %px SPC %py;

		%upos = vectorAdd(vectorSub(vectorAdd(%center, %p), vectorScale(%iconSize SPC %iconSize, 0.5)), %off);
	}

	%world = false;

	if($Pref::Client::mmgDisplayIcons 
	&& ($Pref::Client::mmgDisplayIconsFL || !$mvFreeLook)
	&& !%ico.hideWorld
	&& !MMGRadarLarge.isVisible()
	&& isObject(%player = ServerConnection.getControlObject())
	&& ($Pref::Client::mmgDisplayIconsTPV || !isObject(%player.getControlObject()) || %player.isFirstPerson()))
	{
		%wpos = MMG_worldToScreen(%ico.pos);
		%wpos = mClamp(getWord(%wpos,0),%iconSize,getWord(Canvas.getExtent(),0) - %iconSize) SPC mClamp(getWord(%wpos,1),%iconSize,getWord(Canvas.getExtent(),1)-%iconSize) SPC getWord(%wpos,2);

		if(getWord(%wpos, 2))
			%world = true;

		// todo? clamp positions so off-screen points show up on the edges
		// hi oxy
	}

	%upos = mFloatLength(getWord(%upos, 0), 0) SPC mFloatLength(getWord(%upos, 1), 0);
	%uext = %ico.text.getExtent();
	if(!%ico.hide)
	{
		%ico.resize(getWord(%upos, 0), getWord(%upos, 1), %iconSize, %iconSize);
		%fwpos = vectorSub(%wpos, vectorScale(%iconSize SPC %iconSize, 0.5));
		%ico.world.resize(getWord(%fwpos, 0), getWord(%fwpos, 1), %iconSize, %iconSize);
		%tpos = vectorAdd(%upos, (getWord(%uext, 0) * -0.5 + (%iconSize / 2)) SPC %iconSize - 2);
		%twpos = vectorAdd(%fwpos, vectorSub(%tpos, %upos)); // maintain the same offset between icon/text as the radar
		%ico.text.resize(getWord(%tpos, 0), getWord(%tpos, 1), 64, 64);
		%ico.worldText.resize(getWord(%twpos, 0), getWord(%twpos, 1), getWord(%ico.text.extent, 0), getWord(%ico.text.extent, 1));

		if(%ico.blinkTime > 0)
		{
			%time = getSimTime() / 1000;
			%alpha = mSin((%time * 3.14159) / %ico.blinkTime);
			%alpha = (%alpha / 2) + 0.75;
			%ico.setColor(setWord(%ico.getColor(), 3, %alpha));
			%ico.world.setColor(setWord(%ico.getColor(), 3, %alpha));
		}
		else if(getWord(%ico.getColor(), 3) != 1)
		{
			%ico.setColor(setWord(%ico.getColor(), 3, 1));
			%ico.world.setColor(setWord(%ico.getColor(), 3, 1));
		}
	}
	else
	{
		%tpos = vectorAdd(%upos, (getWord(%uext, 0) * -0.5 + (%iconSize / 2)) SPC 4);
		%ico.text.resize(getWord(%tpos, 0), getWord(%tpos, 1), 64, 64);
	}

	if(!%world)
	{
		%ico.world.setVisible(false);
		%ico.worldText.setVisible(false);
	}
	else
		%ico.worldText.setVisible(true);
}

function MMG_pos2spin(%axis)
{
	%angleOver2 = getWord(%axis,3) * 0.5;
	%angleOver2 = -%angleOver2;
	%sinThetaOver2 = mSin(%angleOver2);
	%cosThetaOver2 = mCos(%angleOver2);
	%q0 = %cosThetaOver2;
	%q1 = getWord(%axis,0) * %sinThetaOver2;
	%q2 = getWord(%axis,1) * %sinThetaOver2;
	%q3 = getWord(%axis,2) * %sinThetaOver2;
	%q0q0 = %q0 * %q0;
	%q1q2 = %q1 * %q2;
	%q0q3 = %q0 * %q3;
	%q2q2 = %q2 * %q2;
	%m21 = 2.0 * (%q1q2 - %q0q3);
	%m22 = 2.0 * %q0q0 - 1.0 + 2.0 * %q2q2;
	
	//clientcmdbottomprint( mRadToDeg(mAsin(%m23)) SPC mRadToDeg(mAtan(-%m13, %m33)) SPC mRadToDeg(mAtan(-%m21, %m22)));
	return mRadToDeg(mAtan(-%m21, %m22));
}

function MMG_pos2dot(%r, %center)
{
	%r= 2 * $pi - (getword(%r,0) * getword(%r,1));
	%rootcos=mcos(%r);
	%rootsin=msin(%r);
	%centerX = getWord(%center, 0);
	%centerY = getWord(%center, 1);
	if($Pref::Client::mmgAlwaysNorth)
	{
		MMGRadarCompassDot.resize((24 * mcos(%r + $pi * -1.5) + %centerX) - 8, (24 * msin(%r + $pi * 1.5) + %centerY) - 8, 16, 16);
	} else {
		MMGRadarCompassDot.resize(((%centerY-9) * mcos(%r + $pi * 1.5) + %centerX) - 8, ((%centerY-9) * msin(%r + $pi * 1.5) + %centerY) - 8, 16, 16);
	}
}

function MMGRadar::tickLoop(%gui)
{
	cancel(%gui.loop);

	if(!%gui.isVisible() && !MMGRadarLargeContainer.isVisible())
		return;

	%delta = (getSimTime() - %gui.lastTick) / 1000;
	%gui.lastTick = getSimTime();
	
	%obj = ServerConnection.getControlObject();
	%form = mmgGetCamera(%obj);
	%pos = getWords(%form, 0, 2);
	if($Pref::Client::mmgAlwaysNorth)
	{
		%fwd = "0 1 0";
	} else {
		%fwd = getWords(%form, 3, 5);
	}
	%up = getWords(%form, 6, 8);
	%right = getWords(%form, 9, 11);
	//echo(%fwd);

	if(!MMGRadarLarge.isVisible())
	{
		%off = %gui.viewOffset;
		%offPos = mmgVector(%off, %fwd);
		%x = getWord(%offPos, 0);
		%offPos = setWord(%offPos, 0, -1 * %x);

		%cts = MMGRadarContainer.getCount();
		%scale = MMGRadar.viewScale;
		for(%i = 0; %i < %cts; %i++)
		{
			%ico = MMGRadarContainer.getObject(%i);
			if(%ico.sourceId $= "")// && %ico != %gui.playerIcon)
				continue;
			
			if($Pref::Client::mmgSquare)
			{
				mmgUpdateIcon(%ico, %pos, %offPos, %fwd, %delta, vectorScale(MMGRadarContainer.getExtent(),0.5), vectorSub(MMGRadar.getExtent(), "16 16"), false, %scale);
			} else {
				mmgUpdateIcon(%ico, %pos, %offPos, %fwd, %delta, $mmgRadarRadius SPC $mmgRadarRadius, $mmgRadarRadius, true, %scale);
			}
		}
		
		if(isObject(%obj))
		{
			if($Pref::Client::mmgCompass)
			{
				if(!$Pref::Client::mmgAlwaysNorth)
				{
					MMGRadarCompass.spin = MMG_pos2spin(getWords(%obj.getTransform(),3,6));
				} else {
					MMGRadarCompass.spin = 360 - MMG_pos2spin(getWords(%obj.getTransform(),3,6));
				}
			}
			if($Pref::Client::mmgCompassDot)
			{
				MMG_pos2dot(getWords(%obj.getTransform(),5,6), $mmgRadarRadius SPC $mmgRadarRadius);
			}
		}
	}
	else
	{
		%off = %gui.largeViewOffset;
		%offPos = mmgVector(%off, %fwd);
		%x = getWord(%offPos, 0);
		%offPos = setWord(%off, 0, -1 * %x);

		%cts = MMGRadarLargeContainer.getCount();
		%scale = MMGRadar.largeViewScale;
		%center = vectorScale(MMGRadarLargeContainer.getExtent(), 0.5);
		for(%i = 0; %i < %cts; %i++)
		{
			%ico = MMGRadarLargeContainer.getObject(%i);
			if(%ico.sourceId $= "")// && %ico != %gui.playerIcon)
				continue;
			
			if($mmgMapDragging)
			{
				%curPos = Canvas.getCursorPos();
				%pt = vectorSub(%curPos, MMGRadarLargeOverlay.getScreenPosition());
				%p = mmgVector(vectorSub(vectorScale(MMGRadarLargeOverlay.getExtent(), 0.5), %pt), %fwd);
				%p = vectorAdd(setWord(%p, 0, -1 * getWord(%p, 0)), %pos);
				%p = vectorScale(%p, $mmgLargeZoom);
				%mapOffset = vectorAdd($mmgMapOffset, vectorSub($mmgStartDragPos,%p));
			} else {
				%mapOffset = $mmgMapOffset;
			}
			
			mmgUpdateIcon(%ico, %pos, %offPos, %fwd, %delta, %center, MMGRadarLargeContainer.getExtent(), false, %scale * $mmgLargeZoom, %mapOffset);
		}
		
		if(isObject(%obj))
		{
			if($Pref::Client::mmgCompass)
			{
				if(!$Pref::Client::mmgAlwaysNorth)
				{
					MMGRadarCompass.spin = MMG_pos2spin(getWords(%obj.getTransform(),3,6));
				} else {
					MMGRadarCompass.spin = 360 - MMG_pos2spin(getWords(%obj.getTransform(),3,6));
				}
			}
			if($Pref::Client::mmgCompassDot)
			{
				MMG_pos2dot(getWords(%obj.getTransform(),5,6), %center);
			}
		}
	}

	%gui.loop = %gui.schedule(1000 / ($fps::real + 5), tickLoop);
}

function clientCmdMMGEnabled(%t)
{
	if(!$mmgActive)
	{
		MMGReset();

		$mmgActive = true;

		MMGRadar.setVisible(true);
		MMGRadarContainer.setVisible(true);
		MMGText.setVisible(true);
		MMGUpdateGui();

		MMGRadar.tickLoop();

		commandToServer('MMGOk', %t);
	}
}

function clientCmdMMGHeadDatablock(%db)
{
	$mmgHeadData = %db;
}

function clientCmdMMGSetText(%a, %b, %c, %d, %e, %f)
{
	MMGText.setValue("<just:right><color:FFFFFF><font:arial:16>" @ %a @ %b @ %c @ %d @ %e @ %f);
}

function MMGCreateIcon(%id)
{
	%ico = new GuiBitmapCtrl(mmgi)
	{
		profile = "GuiDefaultProfile";
		horizSizing = "right";
		vertSizing = "bottom";
		position = "0 0";
		extent = $mmgIconSize SPC $mmgIconSize;
		minExtent = $mmgIconSize SPC $mmgIconSize;
		visible = "1";
		bitmap = "./tex/ico/ico_dot";
		wrap = "0";
		lockAspectRatio = "1";

		sourceId = %id;
	};

	%txt = new GuiMLTextCtrl(mmgit) {
		profile = "mmgTextProfile";
		horizSizing = "right";
		vertSizing = "bottom";
		position = "0 0";
		extent = "64 14";
		minExtent = "64 2";
		visible = "1";
		lineSpacing = "2";
		allowColorChars = "1";
		maxChars = "-1";
		text = " ";
		maxBitmapHeight = "14";
		selectable = "1";

		sourceIcon = %ico;
	};

	%icow = new GuiBitmapCtrl(mmgi)
	{
		profile = "GuiDefaultProfile";
		horizSizing = "right";
		vertSizing = "bottom";
		position = "0 0";
		extent = $mmgIconSize SPC $mmgIconSize;
		minExtent = $mmgIconSize SPC $mmgIconSize;
		visible = "1";
		bitmap = "./tex/ico/ico_dot";
		wrap = "0";
		lockAspectRatio = "1";

		sourceId = %id;
	};

	%txtw = new GuiMLTextCtrl(mmgit) {
		profile = "mmgTextProfile";
		horizSizing = "right";
		vertSizing = "bottom";
		position = "0 0";
		extent = "64 14";
		minExtent = "64 2";
		visible = "1";
		lineSpacing = "2";
		allowColorChars = "1";
		maxChars = "-1";
		text = " ";
		maxBitmapHeight = "14";
		selectable = "1";

		sourceIcon = %ico;
	};

	%ico.text = %txt;
	%ico.world = %icow;
	%ico.worldText = %txtw;

	if(MMGRadar.isVisible())
	{
		MMGRadarContainer.add(%ico);
		MMGRadarContainer.add(%txt);
	}
	else
	{
		MMGRadarLargeContainer.add(%ico);
		MMGRadarLargeContainer.add(%txt);
	}

	MMGWorldIconContainer.add(%icow);
	MMGWorldIconContainer.add(%txtw);

	return %ico;
}

function clientCmdMMGAddIcon(%id, %icon, %text, %color, %pos, %vel, %dist, %blink, %hide)
{
	if(!isObject(MMGRadar.icon[%id]))
	{
		%ico = MMGCreateIcon(%id);

		MMGRadar.icon[%id] = %ico;
		MMGRadar.iconText[%id] = %ico.text;
		MMGRadar.iconWorld[%id] = %ico.world;
		MMGRadar.iconWorldText[%id] = %ico.worldText;
	}

	clientCmdMMGSetIcon(%id, %icon, %text, %color, %dist);
	clientCmdMMGMoveIcon(%id, %pos, %vel, %color);
	clientCmdMMGBlinkIcon(%id, %blink);
	clientCmdMMGHideWorldIcon(%id, %hide);
}

function clientCmdMMGSetIcon(%id, %icon, %text, %color, %dist)
{
	if(!isObject(MMGRadar.icon[%id]))
		return;
	
	if(trim(%color) $= "")
		%color = "1 1 1";

	%color = setWord(%color, 3, 1);

	if(%icon $= "")
		MMGRadar.icon[%id].hide = true;
	else
		MMGRadar.icon[%id].hide = false;

	%file = expandFilename("./tex/ico/ico_" @ %icon @ ".png");

	if(!isFile(%file))
		%file = expandFilename("./tex/ico/ico_dot.png");

	MMGRadar.icon[%id].showDist = %dist;
	MMGRadar.icon[%id].setBitmap(filePath(%file) @ "/" @ fileBase(%file));
	MMGRadar.icon[%id].setColor(%color);
	MMGRadar.iconWorld[%id].setBitmap(filePath(%file) @ "/" @ fileBase(%file));
	MMGRadar.iconWorld[%id].setColor(%color);
	%str = "<just:center><color:" @ rgb2hex(%color) @ ">" @ $Pref::Client::mmgFont;
	MMGRadar.iconText[%id].base = %str;
	MMGRadar.iconText[%id].txt = stripMLControlChars(%text);
	MMGRadar.iconText[%id].setText(%str @ stripMLControlChars(%text));
	MMGRadar.iconWorldText[%id].setText(%str @ stripMLControlChars(%text));
}

function clientCmdMMGBlinkIcon(%id, %time)
{
	if(%time > 0)
		MMGRadar.icon[%id].blinkTime = %time;
	else
		MMGRadar.icon[%id].blinkTime = "";
}

function clientCmdMMGSetViewOffset(%off)
{
	MMGRadar.viewOffset = getWords(vectorScale(%off, 1), 0, 1);
}

function clientCmdMMGSetViewScale(%scale)
{
	if(%scale <= 0)
		%scale = 1;

	MMGRadar.viewScale = (%scale * ($Pref::Client::mmgRadarRadius / 128));
	$MMGRadarScale = %scale;
}

function clientCmdMMGSetLargeViewOffset(%off)
{
	MMGRadar.largeViewOffset = getWords(vectorScale(%off, 1), 0, 1);
}

function clientCmdMMGSetLargeViewScale(%scale)
{
	if(%scale <= 0)
		%scale = 1;

	MMGRadar.largeViewScale = %scale;
}

function clientCmdMMGMoveIcon(%id, %pos, %vel)
{
	MMGRadar.icon[%id].pos = %pos;
	MMGRadar.icon[%id].vel = %vel;
}

function clientCmdMMGClearIcon(%id)
{
	if(isObject(MMGRadar.icon[%id]))
	{
		MMGRadar.icon[%id].delete();
		MMGRadar.iconText[%id].delete();
		MMGRadar.iconWorld[%id].delete();
		MMGRadar.iconWorldText[%id].delete();
	}
}

function clientCmdMMGHideWorldIcon(%id, %hide)
{
	MMGRadar.icon[%id].hideWorld = %hide;
}

function MMGOpenSettings()
{
	$mmgMapDragging = 0;
	Canvas.setCursor(DefaultCursor);
	
	Canvas.pushDialog(MMGSettingsDlg);

	MMGSRadarSize.setValue($Pref::Client::mmgRadarRadius);
	MMGSWorldIcons.setValue($Pref::Client::mmgDisplayIcons);
	MMGSWorldIconsTPV.setValue($Pref::Client::mmgDisplayIconsTPV);
	MMGSWorldIconsFL.setValue($Pref::Client::mmgDisplayIconsFL);
	MMGSAlwaysNorth.setValue($Pref::Client::mmgAlwaysNorth);
	MMGSSquare.setValue($Pref::Client::mmgSquare);
	MMGSCompass.setValue($Pref::Client::mmgCompass);
	MMGSCompassDot.setValue($Pref::Client::mmgCompassDot);
	
	//MMGSCompass.setActive(isBlocklandRebuilt());
	MMGSCompassBlocker.setVisible(!isBlocklandRebuilt());
}

function MMGSaveSettings()
{
	$Pref::Client::mmgRadarRadius = MMGSRadarSize.getValue() * 1;

	if($Pref::Client::mmgRadarRadius <= 64)
		$Pref::Client::mmgRadarRadius = 64;

	$Pref::Client::mmgDisplayIcons = MMGSWorldIcons.getValue();
	$Pref::Client::mmgDisplayIconsTPV = MMGSWorldIconsTPV.getValue();
	$Pref::Client::mmgDisplayIconsFL = MMGSWorldIconsFL.getValue();
	$Pref::Client::mmgAlwaysNorth = MMGSAlwaysNorth.getValue();
	$Pref::Client::mmgSquare = MMGSSquare.getValue();
	$Pref::Client::mmgCompass = MMGSCompass.getValue() && isBlocklandRebuilt();
	$Pref::Client::mmgCompassDot = MMGSCompassDot.getValue();

	Canvas.popDialog(MMGSettingsDlg);
	MMGUpdateGui();
}

function mmg(%p)
{
	exec("./" @ %p @ ".cs");
}


function mmgListIcons()
{
	%dir = "add-ons/script_minimap/tex/ico/ico_*";
	%txt = "<spush><font:Arial Bold:14>Available icons:<spop><br><br><just:left>";
	%filename = findFirstFile(%dir);
	for (%i = 0; %filename !$= ""; %filename = findNextFile(%dir))
	{
		%txt = %txt SPC getSubStr(fileBase(%filename),4,255);
		if(isBlocklandRebuilt())
		{
			%txt = %txt SPC "<bitmap:add-ons/script_minimap/tex/ico/" @ fileBase(%filename) @ ">";
		}
	}
	clientcmdmessageboxok("Minimap Icons List",%txt);
}

if(!$MMGRadarScale)
{
	$MMGRadarScale = 1;
}

MMGUpdateGui();