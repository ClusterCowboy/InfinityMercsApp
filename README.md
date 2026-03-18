
# Infinity Mercs App
This is an app in C# which will allow for the easy creation and managment of a company of mercs for a season. It is under active development right now

## Phase 1
  
### Profiles
 
#### Baseline
 - [X] Peripherals should be split off into separate profiles in Mercs
       Only display mode, with points adjusted
	 - [ ] Give list of valid profiles in army list to link peripherals to
	 - [ ] Have the peripheral show up as equipment in the linked to unit
	 - [X] Only allow one peripheral, subtract cost of additional peripherals       

 - [X] Hyperlinking to weapons, ammo types, equipment, and gear (hyperlinks are part of the data downloaded)
	- [ ]  Weapons, ammo types
 - [X] Show the unit as having Tac orders, Regular Orders, Irregular orders
	- [ ] Show the unit, after being selected as having Lt orders (including multiple)
 - [X] Filter units by stats
 - [X] Filter units by equipment
 - [X] Filter units by skills
 - [X] Filter units by weapons
 - [X] Show the unit as having a cube
 - [X] Show the unit as being hackable
 - [ ] Show the unit as being a peripheral
 - [ ] Show Fireteam options
	 - [X] Show Duo teams
	 - [ ] Highlight options when selecting a type of fireteam
	 - [ ] Show bonuses from fireteams in the statline
#### Mercs Features

 - [X] Spec Ops Implementation
 - [X] Spec Ops for the captain
 - [X] Choosing a captain should give a pool of points for improvement based on the per-scenario options
 - [X] The ability to purchase upgrades and reflect this in the stat line of the units

#### Saving
 - [X] Saving a custom captain
	 - [X] Loading a custom captain
 - [X] Saving a Company
	 - [X] Loading a custom company
 - [X] Saving custom Gear and Skills
	 - [X] Loading custom Gear and Skills from an initial spec ops
	 - [ ] Loading custom Gear and Skills for everything else
 - [ ] Saving Injuries
	 - [ ] Loading Injuries

### Company Types

- [X] Be able to set army points, default to 75 points
- [X] Have a slot at the top for the captain, make it clear
#### Implement Standard Company
- [X] Be able to select two sectorials or one vanilla army
#### Implement Cohesive Company
- [X] Give selection of Core fireteams
- [X] Allow for Level 6 fireteam
- [X] Grant all troopers Number 2
- [X] Change fireteam options to ignore Min trooper restriction
- [X] Have a 3 point discount on any trooper with Tactical Awareness and remove the skill from them
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
 - [X] Show notes and denote when skills are different to how they normally are
 
### Captain
 - [X] Give the ability for the captain to track how many XP is left at the start
 - [X] Give drop down options for the user to improve the skills of the unit
 - [X] Have buttons to click on to add gear
 - [X] Have buttons to click on to add skills
 - [X] Have buttons to click on to add equipment
 - [X] Have buttons to click on to remove anything added
 - [ ] Have a reset button

## Phase 2
 - [ ] Transmutation (Auto) - only allow one form


## Phase 3 +
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

## Known Defects
- [] Create an indicator (yellow circle?) to indicate filter is on
- [] Background Colors not populating correctly based on faction
- [] Evaulation of Fireteam level is not correct