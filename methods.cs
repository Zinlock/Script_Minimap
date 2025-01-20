// Radar view
function GameConnection::MMGSetViewOffset(%cl, %off)
{
	if(isObject(%id))
		%id = %id.getId();

	commandToClient(%cl, 'MMGSetViewOffset', %off);
}

function GameConnection::MMGSetViewScale(%cl, %off)
{
	if(isObject(%id))
		%id = %id.getId();

	commandToClient(%cl, 'MMGSetViewScale', %off);
}

function GameConnection::MMGSetLargeViewOffset(%cl, %off)
{
	if(isObject(%id))
		%id = %id.getId();

	commandToClient(%cl, 'MMGSetLargeViewOffset', %off);
}

function GameConnection::MMGSetLargeViewScale(%cl, %off)
{
	if(isObject(%id))
		%id = %id.getId();

	commandToClient(%cl, 'MMGSetLargeViewScale', %off);
}

function GameConnection::MMGSetText(%cl, %str)
{
	if(isObject(%id))
		%id = %id.getId();

	commandToClient(%cl, 'MMGSetText', getSubStr(%str, 0, 240), getSubStr(%str, 240, 240), getSubStr(%str, 480, 240), getSubStr(%str, 720, 240), getSubStr(%str, 960, 240), getSubStr(%str, 1200, 240));
}

// Other icons
function GameConnection::MMGAddIcon(%cl, %id, %icon, %name, %color, %pos, %vel, %dist, %blink, %hide)
{
	if(isObject(%id))
		%id = %id.getId();

	commandToClient(%cl, 'MMGAddIcon', %id, %icon, %name, %color, %pos, %vel, %dist, %blink, %hide);
}

function GameConnection::MMGSetIcon(%cl, %id, %icon, %name, %color, %dist)
{
	if(isObject(%id))
		%id = %id.getId();

	commandToClient(%cl, 'MMGSetIcon', %id, %icon, %name, %color, %dist);
}

function GameConnection::MMGBlinkIcon(%cl, %id, %time)
{
	if(isObject(%id))
		%id = %id.getId();

	commandToClient(%cl, 'MMGBlinkIcon', %id, %time);
}

function GameConnection::MMGHideWorldIcon(%cl, %id, %hide)
{
	if(isObject(%id))
		%id = %id.getId();

	commandToClient(%cl, 'MMGHideWorldIcon', %id, %hide);
}

function GameConnection::MMGMoveIcon(%cl, %id, %pos, %vel)
{
	if(isObject(%id))
		%id = %id.getId();

	commandToClient(%cl, 'MMGMoveIcon', %id, %pos, %vel);
}

function GameConnection::MMGClearIcon(%cl, %id)
{
	if(isObject(%id))
		%id = %id.getId();

	commandToClient(%cl, 'MMGClearIcon', %id);
}

function GameConnection::MMGCanSee(%cl, %pos)
{
	%con = %cl.getControlObject();

	if(!isObject(%con))
		return false;

	%eye = %con.getEyePoint();
	%vec = %con.getEyeVector();
	%fov = %cl.getControlCameraFOV();

	%dot = vectorDot(%vec, vectorNormalize(vectorSub(%pos, %eye)));

	if(mRadToDeg(mAcos(%dot)) > %fov / 2)
		return false;

	if(isObject(Sky) && vectorDist(%eye, %pos) > (Sky.fogDistance + sky.visibleDistance) / 2)
		return false;

	%mask = $TypeMasks::fxBrickObjectType | $TypeMasks::StaticShapeObjectType | $TypeMasks::InteriorObjectType | $TypeMasks::TerrainObjectType;
	%ray = containerRayCast(%eye, %pos, %mask);

	return !isObject(%ray);
}

function GameConnection::MMGCanScope(%cl, %obj)
{
	if(!isObject(%obj) || !%obj.MMGCanScopeTo(%cl))
		return false;

	if(%obj == %cl.player || %obj == %cl.getControlObject())
		return true;

	if(!isObject(%cl.minigame))
		return true;

	if($Pref::MMG::showEnemy != 4 || $Pref::MMG::showFriendly != 4)
	{
		if(isObject(%cc = %obj.client))
		{
			%team = false;

			if(isObject(%cc.slyrTeam) && isObject(%cl.slyrTeam) && %cc.slyrTeam.isAlliedTeam(%cl.slyrTeam) ||
				 minigameCanDamage(%cl, %obj) != 1)
				%team = true;

			%flag = false;
			if(isObject(%obj.flagSpawn))
				%flag = true;

			if(!%obj.alwaysVisibleTo[%cl])
				%vis = %cl.MMGCanSee(%obj.getCenterPos());
			else
				%vis = true;
			
			if(%team)
			{
				if($Pref::MMG::showFriendly != 0)
				{ // Visible 1 FlagHolder 2 FlagOrVisible 3 Always 4
					if($Pref::MMG::showFriendly == 4 ||
					   %flag && ($Pref::MMG::showFriendly == 2 || $Pref::MMG::showFriendly == 3) ||
						 %vis && ($Pref::MMG::showFriendly == 1 || $Pref::MMG::showFriendly == 3))
						return true;
				}
			}
			else
			{
				if($Pref::MMG::showEnemy != 0)
				{
					if($Pref::MMG::showEnemy == 4 ||
					   %flag && ($Pref::MMG::showEnemy == 2 || $Pref::MMG::showEnemy == 3) ||
						 %vis && ($Pref::MMG::showEnemy == 1 || $Pref::MMG::showEnemy == 3))
						return true;
				}
			}

			return false;
		}
	}

	return true;
}

function ShapeBaseData::MMGCanScopeTo(%db, %obj, %cl)
{
	return true;
}

function ShapeBase::MMGCanScopeTo(%obj, %cl)
{
	return %obj.getDataBlock().MMGCanScopeTo(%obj, %cl);
}

function ShapeBaseData::MMGGetDefaultIcon(%db, %obj)
{
	return "dot_small";
}

function ShapeBase::MMGGetDefaultIcon(%obj)
{
	return %obj.getDataBlock().MMGGetDefaultIcon(%obj);
}

function ShapeBaseData::MMGGetDefaultName(%db, %obj)
{
	if(isObject(%cl = %obj.client))
		return %cl.name;
	else
		return "";
}

function ShapeBase::MMGGetDefaultName(%obj)
{
	return %obj.getDataBlock().MMGGetDefaultName(%obj);
}

function ShapeBaseData::MMGGetDefaultColor(%db, %obj)
{
	%color = "1 1 1";

	if(isObject(%cl = %obj.client))
	{
		if(isObject(%mg = %cl.minigame))
		{
			if(%mg.isSlayerMinigame)
			{
				if(isObject(%cl.slyrTeam))
					%color = getColorIDTable(%cl.slyrTeam.color);
				else
					%color = getColorIDTable(%mg.colorIdx);
			}
			else
				%color = $MinigameColorF[%cl.minigame.colorIdx];
		}
	}

	return %color;
}

function ShapeBase::MMGGetDefaultColor(%obj)
{
	return %obj.getDataBlock().MMGGetDefaultColor(%obj);
}

function ShapeBase::MMGScopeAlwaysTo(%obj, %cl, %icon, %name, %color, %dist, %blink, %hide)
{
	%obj.MMGScopeAlways[%cl] = true;
	%obj.MMGSAIcon[%cl] = %icon;
	%obj.MMGSAName[%cl] = %name;
	%obj.MMGSAColor[%cl] = %color;
	%obj.MMGSADist[%cl] = %dist;
	%obj.MMGSABlink[%cl] = %blink;
	%obj.MMGSAHide[%cl] = %hide;

	if(%cl.MMGCanScope(%obj))
		%obj.MMGScope(%cl, %icon, %name, %color, %dist, %blink, %hide);
}

function ShapeBase::MMGScopeAlwaysAll(%obj, %icon, %name, %color, %dist, %blink, %hide)
{
	%obj.MMGScopeAlwaysAll = true;
	%obj.MMGSAIcon = %icon;
	%obj.MMGSAName = %name;
	%obj.MMGSAColor = %color;
	%obj.MMGSADist = %dist;
	%obj.MMGSABlink = %blink;
	%obj.MMGSAHide = %hide;

	%cts = ClientGroup.getCount();
	for(%i = 0; %i < %cts; %i++)
	{
		%cl = ClientGroup.getObject(%i);

		%obj.MMGScopeAlwaysTo(%cl, %icon, %name, %color, %dist, %blink, %hide);
	}

	if(%cts <= 0)
	{
		if(!isObject(MMGSet))
		{
			new SimSet(MMGSet);
			MissionCleanup.add(MMGSet);
		}
		
		if(!MMGSet.isMember(%obj))
			MMGSet.add(%obj);
	}
}

function ShapeBase::MMGScopeAll(%obj, %icon, %name, %color, %dist, %blink, %hide)
{
	%cts = ClientGroup.getCount();
	for(%i = 0; %i < %cts; %i++)
	{
		%cl = ClientGroup.getObject(%i);

		if(!%obj.MMGScopedTo[%cl] && %cl.MMGCanScope(%obj))
			%obj.MMGScope(%cl, %icon, %name, %color, %dist, %blink, %hide);
	}
}

function ShapeBase::MMGScope(%obj, %cl, %icon, %name, %color, %dist, %blink, %hide)
{
	if(!isObject(%cl) || !%cl.MMGCanScope(%obj))
		return;

	if(%icon $= "")
	{
		%icon = %obj.MMGIcon;

		if(%icon $= "")
			%icon = %obj.MMGGetDefaultIcon();
	}
	else if(%icon $= "none")
		%icon = "";

	if(!isObject(MMGSet))
	{
		new SimSet(MMGSet);
		MissionCleanup.add(MMGSet);
	}
	
	if(!MMGSet.isMember(%obj))
		MMGSet.add(%obj);

	%obj.MMGScopedTo[%cl] = true;

	if(%color $= "")
	{
		%color = %obj.MMGColor;

		if(%color $= "")
			%color = %obj.MMGGetDefaultColor();
	}

	if(%name $= "")
	{
		%name = %obj.MMGName;

		if(%name $= "")
			%name = %obj.MMGGetDefaultName();
	}

	%cl.MMGAddIcon(%obj, %icon, %name, %color, %obj.getCenterPos(), %obj.getVelocity(), %dist, %blink, %hide);
}

function ShapeBase::MMGTick(%obj, %cl)
{
	if(!isObject(%cl))
		return;
	
	if(!%cl.MMGCanScope(%obj))
		return %obj.MMGUnscope(%cl);

	if(%obj.getClassName() $= "Item" && %obj.isStatic())
		%cl.MMGMoveIcon(%obj, %obj.getCenterPos(), "0 0 0");
	else
		%cl.MMGMoveIcon(%obj, %obj.getCenterPos(), %obj.getVelocity());
}

function ShapeBase::MMGUnscope(%obj, %cl)
{
	if(!isObject(%cl))
		return;
	
	if(%obj.MMGScopeAlways[%cl])
		%obj.MMGScopeAlways[%cl] = false;

	if(!%obj.MMGScopedTo[%cl])
		return;
	
	%obj.MMGScopedTo[%cl] = false;
	%cl.MMGClearIcon(%obj);
}

function ShapeBase::MMGUnscopeAll(%obj, %cl)
{
	if(%obj.MMGScopeAlwaysAll)
		%obj.MMGScopeAlwaysAll = false;

	%cts = ClientGroup.getCount();
	for(%i = 0; %i < %cts; %i++)
	{
		%cl = ClientGroup.getObject(%i);

		if(%obj.MMGScopedTo[%cl] || %obj.MMGScopeAlways[%cl])
			%obj.MMGUnscope(%cl);
	}
}

function ShapeBase::MMGBlinkIconTo(%obj, %cl, %time)
{
	%obj.MMGSABlink[%cl] = %time;

	if(%obj.MMGScopedTo[%cl])
		%cl.MMGBlinkIcon(%obj, %time);
}

function ShapeBase::MMGBlinkIconAll(%obj, %time)
{
	%obj.MMGSABlink = %time;

	%cts = ClientGroup.getCount();
	for(%i = 0; %i < %cts; %i++)
	{
		%cl = ClientGroup.getObject(%i);

		if(%obj.MMGScopedTo[%cl])
			%obj.MMGBlinkIconTo(%cl, %time);
	}
}

function ShapeBase::MMGHideWorldIconTo(%obj, %cl, %hide)
{
	%obj.MMGSAHide[%cl] = %hide;

	if(%obj.MMGScopedTo[%cl])
		%cl.MMGHideWorldIcon(%obj, %hide);
}

function ShapeBase::MMGHideWorldIconAll(%obj, %hide)
{
	%obj.MMGSAHide = %hide;

	%cts = ClientGroup.getCount();
	for(%i = 0; %i < %cts; %i++)
	{
		%cl = ClientGroup.getObject(%i);

		if(%obj.MMGScopedTo[%cl])
			%obj.MMGHideWorldIconTo(%cl, %hide);
	}
}

// Callbacks
function SimObject::MMGOnClick(%obj, %cl, %pos)
{
	if(%obj.getClassName() $= "fxDtsBrick")
		%obj.getDataBlock().MMGOnClick(%obj, %cl, %pos);
}

function ShapeBaseData::MMGOnClick(%db, %obj, %cl, %pos)
{
	
}

function ShapeBase::MMGOnClick(%obj, %cl, %pos)
{
	%obj.getDataBlock().MMGOnClick(%obj, %cl, %pos);
}