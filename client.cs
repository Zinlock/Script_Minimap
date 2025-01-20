// i weep

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

exec("./binds.cs");
exec("./ProjectionMatrix.cs"); // Courtesy of Hologlaxer

addKeyBind("Minimap", "Toggle Large Map", MMGToggleLargeMap, "keyboard", "f3", false);
addKeyBind("Minimap", "Show Large Map", MMGShowLargeMap, "", "", false);
addKeyBind("Minimap", "Open Settings", MMGOpenSettings, "keyboard", "ctrl f3", false);

if($Pref::Client::mmgRadarRadius $= "") $Pref::Client::mmgRadarRadius = 128;
if($Pref::Client::mmgDisplayIcons $= "") $Pref::Client::mmgDisplayIcons = true;
if($Pref::Client::mmgDisplayIconsTPV $= "") $Pref::Client::mmgDisplayIconsTPV = false;
if($Pref::Client::mmgDisplayIconsFL $= "") $Pref::Client::mmgDisplayIconsFL = false;

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

	Canvas.popDialog(MMGRadarOverlayContainer);

	clientCmdMMGSetViewOffset("0 0");
	clientCmdMMGSetViewScale(1);
	clientCmdMMGSetLargeViewOffset("0 0");
	clientCmdMMGSetLargeViewScale(1);

	$mmgActive = false;
	$mmgHeadData = -1;
}

function MMGRadarClicked()
{
	%pos = Canvas.getCursorPos();

	%pt = vectorSub(%pos, MMGRadarLargeOverlay.getScreenPosition());
	%obj = ServerConnection.getControlObject();
	%form = mmgGetCamera(%obj);
	%pos = getWords(%form, 0, 2);
	%fwd = getWords(%form, 3, 5);

	%p = mmgVector(vectorSub(vectorScale(MMGRadarLargeOverlay.getExtent(), 0.5), %pt), %fwd);
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
		commandToServer('MMGMapClicked', getWords(%p, 0, 1));
	else
		commandToServer('MMGIconClicked', getWords(%p, 0, 1), %ico.sourceId);
}

package MinimapPkg
{
	function disconnectedCleanup(%bool) // todo?: clean up icons when changing maps too
	{
		MMGReset();

		parent::disconnectedCleanup(%bool);
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

	if(%val)
	{
		dumpSetTo(MMGRadarContainer, MMGRadarLargeContainer);
		MMGRadar.setVisible(false);
		MMGRadarLarge.setVisible(true);

		%pos = MMGRadarLarge.getPosition();
		%ext = MMGRadarLarge.getExtent();
		MMGRadarLargeOverlay.resize(getWord(%pos, 0) + 2, getWord(%pos, 1) + 2, getWord(%ext, 0) - 4, getWord(%ext, 1) - 4);

		Canvas.pushDialog(MMGRadarOverlayContainer);

		// cursorOn();
	}
	else
	{
		dumpSetTo(MMGRadarLargeContainer, MMGRadarContainer);
		MMGRadar.setVisible(true);
		MMGRadarLarge.setVisible(false);
		
		Canvas.popDialog(MMGRadarOverlayContainer);

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

function mmgUpdateIcon(%ico, %pos, %offPos, %fwd, %delta, %center, %size, %rad)
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

	%p = vectorSub(%p, %offPos);
	%p = setWord(vectorSub(%p, %pos), 2, 0);
	%dir = vectorNormalize(%p);

	%dir = mmgVector(%dir, %fwd);
	%y = getWord(%dir, 1);
	%dir = setWord(%dir, 1, -1 * %y);

	if(%rad)
	{
		%len = vectorLen(%p) / MMGRadar.viewScale;

		if(%len > %size)
			%len = %size;
	}
	else
		%len = vectorLen(%p) / MMGRadar.largeViewScale;

	if(%ico.showDist)
	{
		if(%ico.text.txt !$= "")
		{
			%ico.text.setText(%ico.text.base @ %ico.text.txt @ "<br>" @ mFloatLength(vectorDist(%ico.pos, %pos), 0) @ "u");
			%ico.worldText.setText(%ico.text.base @ %ico.text.txt @ "<br>" @ mFloatLength(vectorDist(%ico.pos, %pos), 0) @ "u");
		}
		else
		{
			%ico.text.setText(%ico.text.base @ mFloatLength(vectorDist(%ico.pos, %pos), 0) @ "u");
			%ico.worldText.setText(%ico.text.base @ mFloatLength(vectorDist(%ico.pos, %pos), 0) @ "u");
		}
	}

	if(%rad)
		%off = "30 30";
	else
		%off = "0 0";

	if(%rad)
		%upos = vectorAdd(vectorSub(vectorAdd(%center, vectorScale(%dir, %len)), vectorScale($mmgIconSize SPC $mmgIconSize, 0.5)), %off);
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

		%upos = vectorAdd(vectorSub(vectorAdd(%center, %p), vectorScale($mmgIconSize SPC $mmgIconSize, 0.5)), %off);
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

		if(getWord(%wpos, 2))
			%world = true;

		// todo? clamp positions so off-screen points show up on the edges
	}

	%upos = mFloatLength(getWord(%upos, 0), 0) SPC mFloatLength(getWord(%upos, 1), 0);
	%uext = %ico.text.getExtent();
	if(!%ico.hide)
	{
		%ico.resize(getWord(%upos, 0), getWord(%upos, 1), $mmgIconSize, $mmgIconSize);
		%fwpos = vectorSub(%wpos, vectorScale($mmgIconSize SPC $mmgIconSize, 0.5));
		%ico.world.resize(getWord(%fwpos, 0), getWord(%fwpos, 1), $mmgIconSize, $mmgIconSize);
		%tpos = vectorAdd(%upos, (getWord(%uext, 0) * -0.5 + ($mmgIconSize / 2)) SPC 14);
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
		%tpos = vectorAdd(%upos, (getWord(%uext, 0) * -0.5 + ($mmgIconSize / 2)) SPC 4);
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

function MMGRadar::tickLoop(%gui)
{
	cancel(%gui.loop);

	if(!%gui.isVisible() && !MMGRadarLargeContainer.isVisible())
		return;

	if(MMGWorldIconContainer.isVisible())
		MMGWorldIconContainer.extent = getWords(getRes(), 0, 1);

	%delta = (getSimTime() - %gui.lastTick) / 1000;
	%gui.lastTick = getSimTime();
	
	%obj = ServerConnection.getControlObject();
	%form = mmgGetCamera(%obj);
	%pos = getWords(%form, 0, 2);
	%fwd = getWords(%form, 3, 5);
	%up = getWords(%form, 6, 8);
	%right = getWords(%form, 9, 11);

	if(!MMGRadarLarge.isVisible())
	{
		%off = %gui.viewOffset;
		%offPos = mmgVector(%off, %fwd);
		%x = getWord(%offPos, 0);
		%offPos = setWord(%offPos, 0, -1 * %x);

		%res = getWords(getRes(), 0, 1);
		%size = $Pref::Client::mmgRadarRadius * 2;
		%gOff = "96 -30";
		%nPos = vectorSub(getWord(%res, 0), vectorAdd(%gOff, (%size SPC "0")));

		MMGRadar.resize(getWord(%nPos, 0), getWord(%nPos, 1), %size, %size);
		MMGRadarContainer.resize(getWord(%nPos, 0) - 30, getWord(%nPos, 1) - 30, %size + 60, %size + 60);

		%cts = MMGRadarContainer.getCount();
		for(%i = 0; %i < %cts; %i++)
		{
			%ico = MMGRadarContainer.getObject(%i);
			if(%ico.sourceId $= "")// && %ico != %gui.playerIcon)
				continue;
			
			%center = $Pref::Client::mmgRadarRadius SPC $Pref::Client::mmgRadarRadius;

			mmgUpdateIcon(%ico, %pos, %offPos, %fwd, %delta, %center, $Pref::Client::mmgRadarRadius, true);
		}
	}
	else
	{
		%off = %gui.largeViewOffset;
		%offPos = mmgVector(%off, %fwd);
		%x = getWord(%offPos, 0);
		%offPos = setWord(%off, 0, -1 * %x);

		%cts = MMGRadarLargeContainer.getCount();
		for(%i = 0; %i < %cts; %i++)
		{
			%ico = MMGRadarLargeContainer.getObject(%i);
			if(%ico.sourceId $= "")// && %ico != %gui.playerIcon)
				continue;

			%center = vectorScale(MMGRadarLargeContainer.getExtent(), 0.5);

			mmgUpdateIcon(%ico, %pos, %offPos, %fwd, %delta, %center, MMGRadarLargeContainer.getExtent(), false);
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
		bitmap = "./tex/ico_dot";
		wrap = "0";
		lockAspectRatio = "1";

		sourceId = %id;
	};

	%txt = new GuiMLTextCtrl(mmgit) {
		profile = "GuiMLTextProfile";
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
		bitmap = "./tex/ico_dot";
		wrap = "0";
		lockAspectRatio = "1";

		sourceId = %id;
	};

	%txtw = new GuiMLTextCtrl(mmgit) {
		profile = "GuiMLTextProfile";
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

	%file = expandFilename("./tex/ico_" @ %icon @ ".png");

	if(!isFile(%file))
		%file = expandFilename("./tex/ico_dot.png");

	MMGRadar.icon[%id].showDist = %dist;
	MMGRadar.icon[%id].setBitmap(filePath(%file) @ "/" @ fileBase(%file));
	MMGRadar.icon[%id].setColor(%color);
	MMGRadar.iconWorld[%id].setBitmap(filePath(%file) @ "/" @ fileBase(%file));
	MMGRadar.iconWorld[%id].setColor(%color);
	%str = "<just:center><color:" @ rgb2hex(%color) @ "><font:arial:12>";
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

	MMGRadar.viewScale = %scale;
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
	Canvas.pushDialog(MMGSettingsDlg);

	MMGSRadarSize.setValue($Pref::Client::mmgRadarRadius);
	MMGSWorldIcons.setValue($Pref::Client::mmgDisplayIcons);
	MMGSWorldIconsTPV.setValue($Pref::Client::mmgDisplayIconsTPV);
	MMGSWorldIconsFL.setValue($Pref::Client::mmgDisplayIconsFL);
}

function MMGSaveSettings()
{
	$Pref::Client::mmgRadarRadius = MMGSRadarSize.getValue() * 1;

	if($Pref::Client::mmgRadarRadius <= 64)
		$Pref::Client::mmgRadarRadius = 64;

	$Pref::Client::mmgDisplayIcons = MMGSWorldIcons.getValue();
	$Pref::Client::mmgDisplayIconsTPV = MMGSWorldIconsTPV.getValue();
	$Pref::Client::mmgDisplayIconsFL = MMGSWorldIconsFL.getValue();

	Canvas.popDialog(MMGSettingsDlg);
}

function mmg(%p)
{
	exec("./" @ %p @ ".cs");
}