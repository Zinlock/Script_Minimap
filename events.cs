datablock ItemData(emptyItem : hammerItem)
{
	category = "";
	className = "";

	shapeFile = "base/data/shapes/empty.dts";
	uiName = "";

	doColorShift = false;
	image = "";
};

registerInputEvent("fxDtsBrick", "onBotMMGClicked", "Self fxDtsBrick" TAB "Bot Bot" TAB "Player Player" TAB "Client GameConnection" TAB "Minigame Minigame");
registerInputEvent("fxDtsBrick", "onMMGClicked", "Self fxDtsBrick" TAB "Player Player" TAB "Client GameConnection" TAB "Minigame Minigame");
registerInputEvent("fxDtsBrick", "onVehicleMMGClicked", "Self fxDtsBrick" TAB "Vehicle Vehicle" TAB "Player Player" TAB "Client GameConnection" TAB "Minigame Minigame");

function emptyItem::MMGOnClick(%db, %obj, %cl, %pos)
{
	%brk = %obj.iconBrick;

	if(isObject(%brk) && $Pref::MMG::enableClickEvents)
	{
		$InputTarget_Player = %cl.player;
		$InputTarget_Client = %cl;
		$InputTarget_Minigame = %cl.minigame;
		%brk.processInputEvent("onMMGClicked", %cl);
	}
	else if(%obj.isPingIcon && %cl == %obj.iconClient)
		%obj.delete();
}

function emptyItem::MMGCanScopeTo(%db, %obj, %cc)
{
	if(%obj.isPingIcon)
	{
		%cl = %obj.iconClient;
		if(isObject(%cc.slyrTeam) && isObject(%cl.slyrTeam) && %cc.slyrTeam.isAlliedTeam(%cl.slyrTeam) ||
			minigameCanDamage(%cl, %obj) != 1)
			return true;
		
		return false;
	}

	return Parent::MMGCanScopeTo(%db, %obj, %cc);
}

function Armor::MMGOnClick(%db, %obj, %cl, %pos)
{
	if(!$Pref::MMG::enableClickEvents)
		return;

	%brk = %obj.spawnBrick;

	if(isObject(%brk))
	{
		$InputTarget_Bot = %obj;
		$InputTarget_Player = %cl.player;
		$InputTarget_Client = %cl;
		$InputTarget_Minigame = %cl.minigame;
		%brk.processInputEvent("onBotMMGClicked", %cl);
	}
}

function   HoverVehicleData::MMGOnClick(%db, %obj, %cl, %pos) { WheeledVehicleData::MMGOnClick(%db, %obj, %cl, %pos); }
function  FlyingVehicleData::MMGOnClick(%db, %obj, %cl, %pos) { WheeledVehicleData::MMGOnClick(%db, %obj, %cl, %pos); }
function WheeledVehicleData::MMGOnClick(%db, %obj, %cl, %pos)
{
	if(!$Pref::MMG::enableClickEvents)
		return;

	%brk = %obj.spawnBrick;

	if(isObject(%brk))
	{
		$InputTarget_Vehicle = %obj;
		$InputTarget_Player = %cl.player;
		$InputTarget_Client = %cl;
		$InputTarget_Minigame = %cl.minigame;
		%brk.processInputEvent("onVehicleMMGClicked", %cl);
	}
}

registerOutputEvent("GameConnection", "SetMMGText", "string 200 200\tstring 200 200\tstring 200 200\tstring 200 200");

registerOutputEvent("Player", "SetMMGIcon", "string 32 80\tpaintColor 0");
registerOutputEvent("Bot", "SetMMGIcon", "string 32 80\tstring 32 80\tpaintColor 0");
registerOutputEvent("Vehicle", "SetMMGIcon", "string 32 80\tstring 32 80\tpaintColor 0");
registerOutputEvent("fxDtsBrick", "SetMMGIcon", "string 32 80\tstring 32 80\tpaintColor 0");

registerOutputEvent("Player", "BlinkMMGIcon", "float 0 5 0.1 1");
registerOutputEvent("Bot", "BlinkMMGIcon", "float 0 5 0.1 1");
registerOutputEvent("Vehicle", "BlinkMMGIcon", "float 0 5 0.1 1");
registerOutputEvent("fxDtsBrick", "BlinkMMGIcon", "float 0 5 0.1 1");

function GameConnection::SetMMGText(%cl, %a, %b, %c, %d)
{
	%cl.MMGSetText(%a @ %b @ %c @ %d);
}

function Player::SetMMGIcon(%pl, %icon, %color)
{
	if(%icon $= "none" && %name $= "")
	{
		%pl.MMGUnscopeAll();
		return;
	}

	%color = getColorIDTable(%color);

	if(%icon !$= "")
		%pl.MMGIcon = %icon;

	%pl.MMGColor = %color;
	%pl.MMGScopeAlwaysAll(%icon, %pl.Client.name, %color);
}

function AIPlayer::SetMMGIcon(%pl, %icon, %name, %color)
{
	if(%icon $= "none" && %name $= "")
	{
		%pl.MMGUnscopeAll();
		return;
	}

	%color = getColorIDTable(%color);

	if(%icon !$= "")
		%pl.MMGIcon = %icon;

	if(%name !$= "")
		%pl.MMGName = %name;

	%pl.MMGColor = %color;
	%pl.MMGScopeAlwaysAll(%icon, %name, %color);
}

function Vehicle::SetMMGIcon(%obj, %icon, %name, %color)
{
	if(%icon $= "none" && %name $= "")
	{
		%obj.MMGUnscopeAll();
		return;
	}

	%color = getColorIDTable(%color);

	if(%icon !$= "")
		%obj.MMGIcon = %icon;

	if(%name !$= "")
		%obj.MMGName = %name;

	%obj.MMGColor = %color;
	%obj.MMGScopeAlwaysAll(%icon, %name, %color);
}

function fxDtsBrick::SetMMGIcon(%obj, %icon, %name, %color)
{
	if(%icon $= "none" && %name $= "")
	{
		if(isObject(%obj.iconObject))
			%obj.iconObject.delete();

		return;
	}

	if(!isObject(%obj.iconObject))
	{
		%obj.iconObject = new Item(mmgio)
		{
			datablock = emptyItem;
			position = %obj.getPosition();
			static = true;
			iconBrick = %obj;
		};

		%obj.iconObject.canPickup = false;
	}
	
	%color = getColorIDTable(%color);

	if(%icon !$= "")
		%obj.iconObject.MMGIcon = %icon;

	if(%name !$= "")
		%obj.iconObject.MMGName = %name;

	%obj.iconObject.MMGColor = %color;
	%obj.iconObject.MMGScopeAlwaysAll(%icon, %name, %color);
}

function Player::BlinkMMGIcon(%pl, %time)
{
	%pl.MMGBlinkIconAll(%time);
}

function AIPlayer::BlinkMMGIcon(%pl, %time)
{
	%pl.MMGBlinkIconAll(%time);
}

function Vehicle::BlinkMMGIcon(%obj, %time)
{
	%obj.MMGBlinkIconAll(%time);
}

function fxDtsBrick::BlinkMMGIcon(%obj, %time)
{
	if(isObject(%obj.iconObject))
		%obj.iconObject.MMGBlinkIconAll(%time);
}