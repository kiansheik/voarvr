.PHONY: quest-build quest-run quest-connect quest-install quest-launch

quest-build:
	python3 tools/scripts/unity.py build-quest

quest-run:
	python3 tools/scripts/quest.py run

quest-connect:
	python3 tools/scripts/quest.py connect

quest-install:
	python3 tools/scripts/quest.py install

quest-launch:
	python3 tools/scripts/quest.py launch

push:
	git add .
	git commit
	git push origin HEAD