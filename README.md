# Artifical Intelligence in Computer Games

[TR]
## Özet
Bu proje, Unity ile geliştirdiğim bir "Yapay Zeka Zindanda" fantezisi. Ajanımız her seferinde farklı (rastgele) oluşan odaların içine düşüyor. Amacı; A* algoritmasını kullanarak en kısa yoldan hedefe gitmek ama bu sırada mermisini idareli kullanması ve canına göre taktik yapması gerekiyor.

## Neler Yaptım?
*Procedural Generation:* Her oyunda farklı oda dizilimleri ve koridorlar.

*A Pathfinding:* Ajan, en kısa yolu hesaplayarak dinamik engeller arasından geçer.

*FSM (Kafasına Göre Takılan AI):* Ajan; canı yerindeyse kaçar, canı azsa "agresif" moda geçer ve köşeye sıkışırsa savaşır.

*UI/HUD:* Can barı, mermi sayacı ve kazanma/yeniden başlatma ekranları.

 [ENG]
## What’s this about?
This is a Unity project where an AI agent tries to survive in a procedurally generated dungeon. It’s basically a "smart" agent trying to solve a "random" maze while managing its health and ammo.

## Features
*Dungeon Generator:* No two runs are the same. Random rooms, random corridors, every time.

*A Pathfinding:* The agent doesn't just wander around; it calculates the smartest path to the goal.

*Smart FSM:* An AI with "moods." It flees when healthy but goes into "beast mode" (aggressive) when things get tight.

*Full UI:* Working health bars, ammo counters, and a "Win" screen that actually shows up when it's supposed to.
