.PHONY: quest-run quest-connect quest-install quest-launch

quest-run:
	python3 tools/scripts/quest.py run

quest-connect:
	python3 tools/scripts/quest.py connect

quest-install:
	python3 tools/scripts/quest.py install

quest-launch:
	python3 tools/scripts/quest.py launch
