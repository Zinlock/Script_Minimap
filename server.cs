if(!isFunction(ShapeBaseData, onRemove))
	eval("function ShapeBaseData::onRemove(%db, %obj) { }");

function quickRegisterPref(%name, %cat, %pref, %type, %default)
{
	%pref = trim(%pref);

	if(getSubStr(%pref, 0, 1) !$= "$")
		%pref = "$" @ %pref;

	eval("if(" @ %pref @ " $= \"\") " @ %pref @ " = \"" @ %default @ "\";");

	RTB_registerPref(%name, %cat, %pref, %type, fileBase(filePath($Con::File)), %default, false, false, "");
}

quickRegisterPref("Default View Scale", "Minimap", "$Pref::MMG::defaultScale", "num 0.1 100", 1);
quickRegisterPref("Default Large View Scale", "Minimap", "$Pref::MMG::defaultLargeScale", "num 0.1 100", 1);
quickRegisterPref("Show Visible Friendlies", "Minimap", "$Pref::MMG::showFriendly", "list Never 0 Visible 1 FlagHolder 2 FlagOrVisible 3 Always 4", 4);
quickRegisterPref("Show Visible Enemies", "Minimap", "$Pref::MMG::showEnemy", "list Never 0 Visible 1 FlagHolder 2 FlagOrVisible 3 Always 4", 2);
quickRegisterPref("Enable Flag Icons", "Minimap", "$Pref::MMG::enableFlagIcons", "bool", true);
quickRegisterPref("Enable Icon Click Events", "Minimap", "$Pref::MMG::enableClickEvents", "bool", true);
quickRegisterPref("Enable Player Pings", "Minimap", "$Pref::MMG::enablePings", "bool", true);

function ShapeBase::getCenterPos(%obj)
{
	return %obj.getPosition();
}

function Player::getCenterPos(%pl)
{
	return vectorScale(vectorAdd(%pl.getWorldBoxCenter(), vectorScale(%pl.getPosition(), 3)), 0.25);
}

exec("./support_bots.cs");
exec("./methods.cs");
exec("./events.cs");

datablock PlayerData(MinimapHeadAI : EmptyAI)
{
	UIName = "";
};

package MinimapServerPkg
{
	function GameConnection::onClientEnterGame(%cl, %x, %y)
	{
		if(!isEventPending($mmgt))
			MMGTick();

		%cl.mmghs = getRandom(1, 999999);
		commandToClient(%cl, 'MMGEnabled', %t);
		return Parent::onClientEnterGame(%cl, %x, %y);
	}

	function Armor::onAdd(%db, %obj)
	{
		Parent::onAdd(%db, %obj);

		if(isObject(%obj) && isObject(%obj.client))
		{
			%obj.client.MMGSetViewScale($Pref::MMG::defaultScale);
			%obj.client.MMGSetViewScale($Pref::MMG::defaultLargeScale);
			%obj.schedule(0, MMGScopeAlwaysAll, "triangle");
			%obj.schedule(0, MMGHideWorldIconAll, true);

			if(%obj.client.hasMMG)
				%obj.minimapHead = %obj.mountMXBot(5, MinimapHeadAI);
		}
	}

	function Armor::onDisabled(%db, %obj, %state)
	{
		if(isObject(%obj) && %obj.MMGScopeAlwaysAll)
			%obj.MMGScopeAlwaysAll("cross");

		Parent::onDisabled(%db, %obj, %state);
	}

	function Armor::onRemove(%db, %obj)
	{
		%obj.MMGUnscopeAll();

		Parent::onRemove(%db, %obj);
	}

	function ShapeBaseData::onRemove(%db, %obj)
	{
		%obj.MMGUnscopeAll();

		Parent::onRemove(%db, %obj);
	}

	function fxDtsBrick::onRemove(%obj)
	{
		if(isObject(%ico = %obj.iconObject))
			%ico.delete();
		
		Parent::onRemove(%obj);
	}

	function destroyServer()
	{
		cancel($mmgt);
		return Parent::destroyServer();
	}

	function Slayer_CTF::onFlagPickup(%this, %client, %team, %brick, %flag)
	{
		if(isObject(%pl = %client.player) && $Pref::MMG::enableFlagIcons)
			%pl.MMGScopeAlwaysAll("flag", "", "", true, 0.3);

		Parent::onFlagPickup(%this, %client, %team, %brick, %flag);
	}

	function Slayer_CTF::onFlagReturn(%this, %client, %team, %brick, %flag)
	{
		if(isObject(%pl = %client.player) && $Pref::MMG::enableFlagIcons)
		{
			%pl.MMGScopeAlwaysAll("triangle");
			%pl.schedule(50, MMGBlinkIconAll, 0);
		}

		Parent::onFlagReturn(%this, %client, %team, %brick, %flag);
	}

	function Slayer_CTF::onFlagDrop(%this, %client, %team, %brick, %flag)
	{
		if(isObject(%pl = %client.player) && $Pref::MMG::enableFlagIcons)
		{
			%pl.MMGScopeAlwaysAll("triangle");
			%pl.schedule(50, MMGBlinkIconAll, 0);
		}

		Parent::onFlagDrop(%this, %client, %team, %brick, %flag);

		if($Pref::MMG::enableFlagIcons)
			%flag.MMGScopeAlwaysAll("flag", "", getColorIDTable(%brick.getColorID()));
	}
};
activatePackage(MinimapServerPkg);

function serverCmdMMGOk(%cl)
{
	%cl.MMGSetViewScale($Pref::MMG::defaultScale);
	%cl.MMGSetViewScale($Pref::MMG::defaultLargeScale);
	commandToClient(%cl, 'MMGHeadDatablock', MinimapHeadAI.getId());
	%cl.hasMMG = true;
}

if($Version $= "21")
	eval("function getTerrainHeight(){return 0;}");

function getHighestPos(%pos)
{
	%pos = setWord(%pos, 2, getTerrainHeight(%pos));

	return posFromRaycast(containerRayCast(%pos, vectorAdd(%pos, "0 0 1000"), $TypeMasks::fxBrickObjectType | $TypeMasks::StaticShapeObjectType | $TypeMasks::InteriorObjectType | $TypeMasks::TerrainObjectType));
}

function MMGTick()
{
	cancel($mmgt);

	if(!isObject(MMGSet))
	{
		new SimSet(MMGSet);
		MissionCleanup.add(MMGSet);
	}

	%cts = MMGSet.getCount();
	for(%i = 0; %i < %cts; %i++)
	{
		%obj = MMGSet.getObject(%i);

		%ct2 = ClientGroup.getCount();
		for(%o = 0; %o < %ct2; %o++)
		{
			%cl = ClientGroup.getObject(%o);

			if(!%cl.hasMMG)
				continue;

			// if(getSimTime() - %cl.lastMMH > 50)
			// {
			// 	%cl.lastMMH = getSimTime();

			// 	if(isObject(%control = %cl.getControlObject()))
			// 	{
			// 		commandToClient(%cl, 'MMGEyeVector', %control.getEyeVector(), %control.getEyeTransform());

			// 		if(%cl.minimapdebug)
			// 		{
			// 			drawArrow(%control.getCenterPos(), %control.rotation, "1 1 1 1", 5).schedule(200, delete);
			// 		}
			// 	}
			// }

			if(%obj.MMGScopedTo[%cl])
			{
				%obj.MMGTick(%cl);
			}
			else if(%obj.MMGScopeAlways[%cl])
			{
				if(%cl.MMGCanScope(%obj))
					%obj.MMGScope(%cl, %obj.MMGSAIcon[%cl], %obj.MMGSAName[%cl], %obj.MMGSAColor[%cl], %obj.MMGSADist[%cl], %obj.MMGSABlink[%cl], %obj.MMGSAHide[%cl]);
			}
			else if(%obj.MMGScopeAlwaysAll)
			{
				%obj.MMGSAIcon[%cl] = %obj.MMGSAIcon;
				%obj.MMGSAName[%cl] = %obj.MMGSAName;
				%obj.MMGSAColor[%cl] = %obj.MMGSAColor;
				%obj.MMGSADist[%cl] = %obj.MMGSADist;
				%obj.MMGSABlink[%cl] = %obj.MMGSABlink;
				%obj.MMGSAHide[%cl] = %obj.MMGSAHide;

				if(%cl.MMGCanScope(%obj))
					%obj.MMGScope(%cl, %obj.MMGSAIcon[%cl], %obj.MMGSAName[%cl], %obj.MMGSAColor[%cl], %obj.MMGSADist[%cl], %obj.MMGSABlink[%cl], %obj.MMGSAHide[%cl]);
			}
		}
	}

	$mmgt = schedule(100, 0, MMGTick);
}

function serverCmdMMGMapClicked(%cl, %pos)
{
	if($Pref::MMG::enablePings)
	{
		if(!isObject(%obj = %cl.pingObject))
		{
			%obj = new Item(mmgio)
			{
				datablock = emptyItem;
				position = %pos;
				static = true;
				iconClient = %cl.getId();
				isPingIcon = true;
			};

			%cl.pingObject = %obj;

			%obj.canPickup = false;

			%obj.cleanup = %obj.schedule(30000, delete);
		}
		else
		{
			cancel(%obj.cleanup);
			%obj.cleanup = %obj.schedule(30000, delete);
		}

		%obj.setTransform(%pos SPC getTerrainHeight(%pos));
		%obj.MMGScopeAlwaysAll("marker", %cl.name @ "'s ping", "", true, 0.5);
	}
}

function serverCmdMMGIconClicked(%cl, %pos, %ico)
{
	if(isObject(%ico))
		%ico.MMGOnClick(%cl, %pos);
}