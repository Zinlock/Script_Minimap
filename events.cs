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
$OutputDescription_["GameConnection", "SetMMGText"] = "[text] [text] [text] [text]" NL
														"Displays text under the player's minimap (if they have the mod)" NL
														"text (800): All 4 textboxes are put together.";

registerOutputEvent("Player", "SetMMGIcon", "string 32 80\tpaintColor 0");
$OutputDescription_["Player", "SetMMGIcon"] = "[string] [color]" NL
												"Sets the player's minimap icon." NL
												"string: Type mmgListIcons(); in console." NL
												"color: Colorset color to use";

registerOutputEvent("Bot", "SetMMGIcon", "string 32 80\tstring 32 80\tpaintColor 0");
$OutputDescription_["Bot", "SetMMGIcon"] = "[string] [name] [color]" NL
												"Sets the bot's minimap icon." NL
												"string: Type mmgListIcons(); in console." NL
												"name: Sets the icon's name." NL
												"color: Colorset color to use";
												
registerOutputEvent("Vehicle", "SetMMGIcon", "string 32 80\tstring 32 80\tpaintColor 0");
$OutputDescription_["Vehicle", "SetMMGIcon"] = "[string] [name] [color]" NL
												"Sets the vehicle's minimap icon." NL
												"string: Type mmgListIcons(); in console." NL
												"name: Sets the icon's name." NL
												"color: Colorset color to use";
												
registerOutputEvent("fxDtsBrick", "SetMMGIcon", "string 32 80\tstring 32 80\tpaintColor 0");
$OutputDescription_["fxDtsBrick", "SetMMGIcon"] = "[string] [name] [color]" NL
												"Sets the brick's minimap icon." NL
												"string: Type mmgListIcons(); in console." NL
												"name: Sets the icon's name." NL
												"color: Colorset color to use";

registerOutputEvent("Player", "BlinkMMGIcon", "float 0 5 0.1 1");
$OutputDescription_["Player", "BlinkMMGIcon"] = "[slider]" NL
												"Blinks the player's minimap icon." NL
												"slider: Sets how long it takes to blink, or 0 to disable.";
												
registerOutputEvent("Bot", "BlinkMMGIcon", "float 0 5 0.1 1");
$OutputDescription_["Bot", "BlinkMMGIcon"] = "[slider]" NL
												"Blinks the bot's minimap icon." NL
												"slider: Sets how long it takes to blink, or 0 to disable.";

registerOutputEvent("Vehicle", "BlinkMMGIcon", "float 0 5 0.1 1");
$OutputDescription_["Vehicle", "BlinkMMGIcon"] = "[slider]" NL
												"Blinks the vehicle's minimap icon." NL
												"slider: Sets how long it takes to blink, or 0 to disable.";

registerOutputEvent("fxDtsBrick", "BlinkMMGIcon", "float 0 5 0.1 1");
$OutputDescription_["fxDtsBrick", "BlinkMMGIcon"] = "[slider]" NL
												"Blinks the bricks's minimap icon." NL
												"slider: Sets how long it takes to blink, or 0 to disable.";


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