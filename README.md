# PSI-project- 🪢 KNOTS – Interactive Icebreaker Game
5th group's students of VU project. Members: Roberta Tamaševskytė, Emilija Sankauskaitė, Kamilė Maleiškaitė, Rokas Kancius, Ugnė Jurkšaitytė.


## 🎯 Project Idea

🪢 KNOTS is an interactive icebreaker game designed to bring people closer together. 

- Split players into groups.  
- Each player answers **5 questions** (configurable).  
- When everyone finishes, all group members **vote** (like/dislike) on each answer.  
- Based on the votes, the system generates a **matching table** showing compatibility between players.  
- The matches help spark conversations and create opportunities for real-life connections.  

---

## 🔐 Static assets policy

- Bootstrap is loaded from CDN in `KNOTS/Components/App.razor`.
- Keep `integrity` and `crossorigin` attributes on CDN styles/scripts for Subresource Integrity (SRI).
- If the Bootstrap version is changed, update both the CDN URL and the matching SRI hash.
- Application-owned assets remain in `KNOTS/wwwroot/` (`app.css`, `js/`, `images/`, `favicon.png`).


## 🎯 Our plan

### 🔹 Alpha version
- Basic UI for answering questions.  
- Formation of players group.  
- Voting system (like/dislike on each answer).  
- Simple matching algorithm.  
- Results displayed in a basic table.  

### 🔹 Beta version  
- Ability to choose questions topics (sports, movies, etc.).
- Polished UI with animations.  
 

### 🔹 Final version
- Smarter matchmaking suggestions (recommend groups or individuals).  
- Support for custom questions.  
- Working version without any 🪲.
  

---
