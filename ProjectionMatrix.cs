//Support code functions for projection matrix for the damage popup feature.

//Before I continue I would just like to thank the devs on the BlockOps team
//for writing these functions, go support them for this brainfuckery of maths.

// * Further modified by Oxy (260031) for minimaps
// - Renamed all functions to avoid conflicts
// - Fixed camera tilt offsetting waypoints
// - Fixed vehicle third person camera offset not being taken into account

function MMG_lerp2i(%start, %end, %dt)
{
	%v = vectorAdd(%start, vectorScale(vectorSub(%end, %start), %dt));
	return mFloor(getWord(%v, 0) + 0.5) SPC mFloor(getWord(%v, 1) + 0.5);
}

//from Johhny https://forum.blockland.us/index.php?topic=209588.msg5869436#msg5869436

//Taking some liberty to move some stuff from this function that can be pre-computed beforehand...
//Since we will be projecting alot of possible damage popups, so I would like a little extra performance out of this if I can.
function MMG_worldToScreen(%worldPosition)
{
	%player = ServerConnection.getControlObject();

	if(%player.getType() & $TypeMasks::PlayerObjectType && !%player.isFirstPerson())
	{
		%muzzleVector = %player.MMG_getThirdPersonEyeVector();
		%eyePoint = %player.MMG_getThirdPersonCameraPos();
	} else {
		%muzzleVector = %player.MMG_getBestMuzzleVector();
		%eyePoint = MMG_getMyEyePoint();
	}

	%eyeTransform = MMG_getMyEyeTransform(%muzzleVector, %eyePoint);
	%fov = mTan((PlayGui.forceFov || $cameraFoV) * $pi / 360);

	%screenWidth = getWord(getRes(), 0);
	%screenHeight = getWord(getRes(), 1);

	%offset = MatrixMulVector("0 0 0" SPC getWords(%eyeTransform, 3, 5) SPC -1 * getWord(%eyeTransform, 6), VectorSub(%worldPosition, %eyeTransform));
	%x = getWord(%offset,0);
	%y = getWord(%offset,1);
	%z = getWord(%offset,2);

	%fovFactor = %y * %fov;

	%screenX = ((%x / %fovFactor) + 1) / 2 * %screenWidth;
	%screenY = %screenHeight - ((%z / %fovFactor * %screenWidth) - %screenHeight) / -2;

	return mFloor(%screenX + 0.5) SPC mFloor(%screenY + 0.5) SPC (vectorDot(vectorNormalize(vectorSub(%worldPosition, %eyeTransform)), %muzzleVector) > 0);
}

// BY Xalos, from Client_RealisticSpace
function MMG_getMyEyeTransform(%MV, %cameraPosition)
{
	%player = serverConnection.getControlObject();
	%FV = vectorNormalize(setWord(%MV, 2, 0));

	%yaw = mAtan(getWord(%FV, 0), getWord(%FV, 1)) + 3.14159265;

	if(%yaw >= 3.14159265)
		%yaw -= 6.2831853;

	%roll = 0;

	if(%player.isMounted())
	{
		%globalRV = vectorCross("0 0 1", %FV);
		%RV = vectorCross(%player.getObjectMount().getUpVector(), %MV);

		%roll = 180 * mAcos(vectorDot(%globalRV, %RV)) / $pi;
		if(getWord(%RV, 2) > 0)
			%roll *= -1;
	}

	return %cameraPosition SPC eulerToAxis(mAsin(vectorDot(vectorNormalize(%MV), "0 0 1")) * -57.2958 @ " " @ %roll @ " " @ %yaw * -57.2958);
}

//dont remember where i got this from?
function MMG_getMyHackPosition(%player)
{
	%hackOffset = 1.32504 * getWord(%player.getScale(), 2);

	%pos = vectorAdd(%player.getPosition(), "0 0" SPC %hackOffset);
	return %pos;
}

		// %hackOffset = 0.20344 * (getWord(%player.getObjectBox(), 5) / 4);
//by buddy - excludes crouching
function MMG_getMyEyePoint()
{
	%player = serverConnection.getControlObject();
	if(!isObject(%player))
		return "0 0 0";

	//not a player
	if(!(%player.getType() & $TypeMasks::PlayerObjectType))
		return getWords(%player.getTransform(), 0, 2);

	%m = %player.getTransform();

	%scale = %player.getScale();
	%eyeNodeOffset = -0.00211614 * getWord(%scale, 0)
				  SPC 0.141104   * getWord(%scale, 1)
				  SPC 2.1565     * getWord(%scale, 2);

	%v = MatrixMulPoint(%player.getTransform(), %eyeNodeOffset);

	return getWords(%v, 0, 2);
}

function SimObject::MMG_getBestMuzzleVector(%this)
{
	if(!(%this.getType() & $TypeMasks::PlayerObjectType))
		return %this.getMuzzleVector(0);

	if($mvFreeLook || (isObject(%this.getControlObject()) && !%this.isFirstPerson()))
	{
		%cts = %this.getMountedObjectCount();
		for(%i = 0; %i < %cts; %i++)
		{
			%mount = %this.getMountedObject(%i);

			if(%mount.getDataBlock() == $mmgHeadData)
			{
				%mount.setTransform("0 0 0 0 0 0 0"); // for some reason, this thing just Never updates on its own when the player is standing still
				return %mount.getMuzzleVector(0);
			}
		}
	}

	for(%i = 0; %i < 4; %i++)
	{
		if(%this.getMountedImage(%i) == 0)
			return %this.getMuzzleVector(%i);
	}

	return %this.getMuzzleVector(3);
}

function Player::MMG_getThirdPersonEyeVector(%player)
{
	if(isObject(%vehicle = %player.getControlObject()))
	{
		%eyeVector = %player.MMG_getBestMuzzleVector(); //eye vector

		if(!%vehicle.getDataBlock().cameraRoll)
			%eyeVector = vectorNormalize(setWord(%eyeVector, 2, 0));

		%Yaw = mAtan(getWord(%eyeVector, 1), getWord(%eyeVector, 0));
		%Pitch = mAcos(getWord(%eyeVector, 2)) + %vehicle.getDataBlock().cameraTilt;
	}
	else
	{
		%eyeVector = %player.MMG_getBestMuzzleVector(); //eye vector
		%Yaw = mAtan(getWord(%eyeVector, 1), getWord(%eyeVector, 0));
		%Pitch = mAcos(getWord(%eyeVector, 2)) + %player.getDataBlock().cameraTilt;
	}

	%x = 1 * mSin(%Pitch) * mCos(%Yaw);
	%y = 1 * mSin(%Pitch) * mSin(%Yaw);
	%z = 1 * mCos(%Pitch);

	return vectorNormalize(%x SPC %y SPC %z);
}

function Player::MMG_getThirdPersonCameraPos(%player, %ignoreRaycast)
{
	%db = %player.getDatablock();
	%pos = MMG_getMyHackPosition(%player); //%player.getHackPosition();
	%eye = %player.MMG_getThirdPersonEyeVector();
	%scale = %player.getScale();

	if(isObject(%vehicle = %player.getControlObject()))
	{
		%db = %vehicle.getDataBlock();
		%dist = -%db.cameraMaxDist * getWord(%player.getScale(), 1);
		%off = vectorScale(%eye, %dist);
		%off = setWord(%off, 2, getWord(%off, 2) + %db.cameraOffset);
	}
	else
	{
		%dist = -%db.cameraMaxDist * getWord(%player.getScale(), 1);
		%off = vectorScale(%eye, %dist);
		%off = setWord(%off, 2, getWord(%off, 2) + %db.cameraVerticalOffset);
	}

	%outpos = vectorAdd(%pos, %off);

	return %outpos;
}

function eulerToAxis(%euler)
{
	%euler = VectorScale(%euler, $pi / 180);
	%matrix = MatrixCreateFromEuler(%euler);
	return getWords(%matrix, 3, 6);
}