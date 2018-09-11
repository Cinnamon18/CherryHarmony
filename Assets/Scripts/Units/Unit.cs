﻿using System.Collections.Generic;
using UnityEngine;
using Constants;
using Gameplay;
using System;
using AI;
using System.Linq;
using UnityEngine.UI;
using Buffs;

namespace Units {
	public abstract class Unit : MonoBehaviour, IBattlefieldItem {
		private const int DEFAULT_DAMAGE = 10;
		private const int DEFAULT_HEALTH = 100;

		private readonly ArmorType armor;
		private readonly WeaponType weapon;
		private readonly MoveType moveType;
		private readonly UnitType unitType;
		private readonly int maxHealth;
		private int health;
		public List<Buff> buffs;
		public bool hasMovedThisTurn;
		//How many tiles this unit can move per turn turn
		private int numMoveTiles { get; set; }

		[SerializeField]
		private Image healthBar;

		public Unit(ArmorType armorType, WeaponType weaponType, MoveType moveType, UnitType unitType) {
			armor = armorType;
			weapon = weaponType;
			this.moveType = moveType;
			this.unitType = unitType;
			buffs = new List<Buff>();

			maxHealth = DEFAULT_HEALTH;
			health = DEFAULT_HEALTH;
			hasMovedThisTurn = false;
			this.numMoveTiles = unitType.unitMoveDistance();
		}

		void Start() {

		}
		void Update() {

		}

		public Character getCharacter(Battlefield battlefield) {
			Character myCharacter = null;
			foreach (Character character in battlefield.charactersUnits.Keys) {
				if (battlefield.charactersUnits[character].Contains(this)) {
					myCharacter = character;
				}
			}
			return myCharacter;
		}

		//returns true if the enemy was destroyed by battle
		public bool doBattleWith(Unit enemy, Tile enemyTile, Battlefield battlefield) {
			float damage = this.weapon.baseDamage * (1f * this.health / this.maxHealth);
			damage = damage * ((100 - this.weapon.damageType.DamageReduction(enemy.armor)) / 100.0f);
			damage = damage * ((100 - enemyTile.tileType.DefenseBonus()) / 100.0f);

			List<Buff> damageBuffs = buffs.FindAll( buff => buff.GetType() == typeof(DamageBuff));
			foreach (Buff buff in damageBuffs) {
				damage = damage *= (buff as DamageBuff).getDamageBonus();
			}

			//Damage rounds up
			enemy.health -= (int)(Mathf.Ceil(damage));
			enemy.healthBar.fillAmount = 1f * enemy.health / enemy.maxHealth;

			if (enemy.health <= 0) {
				enemy.defeated(battlefield);
				return true;
			} else {
				return false;
			}

		}

		public void defeated(Battlefield battlefield) {
			Destroy(this.gameObject);
			battlefield.charactersUnits[this.getCharacter(battlefield)].Remove(this);
		}

		/*
		For now this will use a simple percolation algorithm using a visited set instead of a disjoint set approach
		We can get away with this because there's only one "flow" source point (the unit).
		 */
		public List<Coord> getValidMoves(int myX, int myY, Battlefield battlefield) {

			HashSet<Coord> visited = new HashSet<Coord>();
			PriorityQueue<AIMove> movePQueue = new PriorityQueue<AIMove>();
			movePQueue.Enqueue(new AIMove(myX, myY, 0));
			while (movePQueue.Count() > 0) {
				AIMove currentMove = movePQueue.Dequeue();
				//check all four directions
				int[,] moveDirs = new int[,] { { 0, 1 }, { 0, -1 }, { 1, 0 }, { -1, 0 } };

				for (int x = 0; x < moveDirs.GetLength(0); x++) {
					int targetX = currentMove.x + moveDirs[x, 0];
					int targetY = currentMove.y + moveDirs[x, 1];
					if (targetX < battlefield.map.GetLength(0) && targetY < battlefield.map.GetLength(1) && targetX >= 0 && targetY >= 0) {
						Stack<Tile> targetTile = battlefield.map[targetX, targetY];

						int movePointsExpended = currentMove.weight + targetTile.Peek().tileType.Cost(this.moveType);
						Coord targetMove = new Coord(targetX, targetY);
						AIMove targetMoveAI = new AIMove(targetX, targetY, movePointsExpended);

						if (movePointsExpended <= unitType.unitMoveDistance()) {
							if (!visited.Contains(targetMove)) {
								visited.Add(targetMove);
								movePQueue.Enqueue(targetMoveAI);
							}
						}
					}
				}
			}

			return visited.ToList();
		}

		public List<Unit> getTargets(int myX, int myY, Battlefield battlefield, Character character) {
			//TODO: Replace this with an actual implementation taking into account range and junk.
			List<Unit> targets = new List<Unit>();
			List<Coord> tiles = getValidMoves(myX, myY, battlefield);
			foreach(Coord tile in tiles) {
				Unit targetUnit = battlefield.units[tile.x, tile.y];
				if(targetUnit != null && targetUnit.getCharacter(battlefield) != character) {
					targets.Add(targetUnit);
				}
			}
			return targets;
		}
	}
}