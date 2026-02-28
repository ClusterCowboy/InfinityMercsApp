
# Infinity Mercs App
This is an app in C# which will allow for the easy creation and managment of a company of mercs for a season. It is under active development right now

## Phase 1
  
### Profiles
 
#### Baseline
 - [ ] Peripherals should be split off into separate profiles in Mercs
       Only display mode, with points adjusted
	 - [ ] Give list of valid profiles in army list to link peripherals to
	 - [ ] Have the peripheral show up as equipment in the linked to unit
	 - [ ] Only allow one peripheral, subtract cost of additional peripherals       
 - [X] Hyperlinking to weapons, ammo types, equipment, and gear (hyperlinks are part of the data downloaded)
	- [ ]  Weapons, ammo types
 - [X] Show the unit as having Tac orders, Regular Orders, Irregular orders
	- [ ] Show the unit, after being selected as having Lt orders (including multiple)
 - [X] Show the unit as having a cube
 - [X] Show the unit as being hackable
 - [ ] Show the unit as being a peripheral
 - [ ] Show Fireteam options
	 - [ ] Show Duo, Haris, or Core teams
	 - [ ] Highlight options when selecting a type of fireteam
	 - [ ] Show bonuses from fireteams in the statline
#### Mercs Features
 - [ ] Spec Ops Implementation
 - [ ] Spec Ops for the captain
 - [ ] Choosing a captain should give a pool of points for improvement based on the per-scenario options
 - [ ] The ability to purchase upgrades and reflect this in the stat line of the unit
#### Saving
 - [ ] Saving a custom captain
	 - [ ] Loading a custom captain
 - [ ] Saving a Company
	 - [ ] Loading a custom company
 - [ ] Saving custom Gear and Skills
	 - [ ] Loading custom Gear and Skills
 - [ ] Saving Injuries
	 - [ ] Loading Injuries

### Company Types

#### Implement Standard Company
 - [ ] Be able to select two sectorials or one vanilla army
#### Implement Cohesive Company
- [ ] Give selection of Core fireteams
- [ ] Allow for Level 6 fireteam
- [ ] Grant all troopers Number 2
- [ ] Change fireteam options to ignore Min trooper restriction
- [ ] Have a 3 point discount on any trooper with Tactical Awareness and remove the skill from them
#### Implement Inspiring Leader
- [ ] Prompt Selection of Lieutenant from Sectorial
- [ ] Give Lieutenant Inspiring Leadership
- [ ] Allow for selection of any irregular trooper from any list
	- [ ] Ensure AVA is highest possible
#### Airborne Company
- [ ] Prompt Selection of Lieutenant from Sectorial
- [ ] Give Lieutenant Parachutist and Network Support (Controlled Jump)
- [ ] Allow for selection of only troopers with the airborne deployment from any list
- [ ] Denote the use of one speedball per contract
#### TAG Company
- [ ] Allow purchase of a standard TAG
- [ ] Allow choosing TAG as Lt
- [ ] Restrict specific skills from the TAG
- [ ] Allow spending of 20 spec ops points on TAG
- [ ] Allow purchase of other models
#### Proxy Pack
- [ ] Choose a single captain from any vanilla or sectorial (Lt not required, must be regular trooper)
- [ ] Grant additional 10 XP
- [ ] On level up, grant 2 perks and 10 renown
- [ ] Doctor between rounds, even if unconscious
- [ ] Always Elite Deploy with Tac Aware
- [ ] Allow for selections of other profiles of the captain as other units, no other units allowed for purchase
	- [ ] Allow these to provide additional orders
- [ ] Implement Aspect of the Wolf Skill

### Company Stats
 - [ ] Renown
 - [ ] Notoriety

### Custom Skills
 - [ ] Show notes and denote when skills are different to how they normally are

## Phase 2+
 - [ ] Season management
	 - [ ] Import/Export profiles
 - [ ] Classified Objective picker
 - [ ] Inducements 
	 - [ ] Calculator
	 - [ ] Troops for hire
	 - [ ] Command Tokens
	 - [ ] Rented Equipment
 - [ ] Company Deployment
 - [ ] Experience Points during play
And more