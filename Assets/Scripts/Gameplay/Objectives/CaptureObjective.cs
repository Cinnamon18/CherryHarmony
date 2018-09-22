﻿using System.Collections.Generic;
using Units;

namespace Gameplay {
	public class CaptureObjective : GameObjective {

		public List<Coord> capturePoints = new List<Coord>();
		public int timeToHold = 0;
		private int timeHeld = 0;
		private int lastHalfTurnsElapsed = 0;

		public CaptureObjective(Battlefield battlefield, Level level, Character playerCharacter, int maxHalfTurns) :
			base(battlefield, level, playerCharacter, maxHalfTurns) { }

		public CaptureObjective(Battlefield battlefield, Level level, Character playerCharacter, int maxHalfTurns, List<Coord> capturePoints, int timeToHold) :
			base(battlefield, level, playerCharacter, maxHalfTurns) {
				this.capturePoints = capturePoints;
				this.timeToHold = timeToHold;
			 }

		public override bool isLoseCondition(int halfTurnsElapsed) {
			if (battlefield.charactersUnits[playerCharacter].Count == 0) {
				return true;
			} else if (halfTurnsElapsed >= maxHalfTurns) {
				return true;
			}
			return false;
		}

		public override bool isWinCondition(int halfTurnsElapsed) {
			foreach (Coord coord in this.capturePoints) {
				Unit unit = battlefield.units[coord.x, coord.y];
				if (unit == null || unit.getCharacter(battlefield) != playerCharacter) {
					timeHeld = 0;
					return false;
				}
			}
			if (lastHalfTurnsElapsed < halfTurnsElapsed) {
				timeHeld++;
				lastHalfTurnsElapsed = halfTurnsElapsed;
			}
			return timeHeld >= timeToHold;
		}
	}
}
