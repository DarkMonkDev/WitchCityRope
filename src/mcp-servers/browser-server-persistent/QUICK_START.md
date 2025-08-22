# Browser Server Quick Start Guide

## 1. Start the Server

```bash
cd /mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/browser-server-persistent
./browser-server-manager.sh start
```

## 2. Check Status

```bash
./browser-server-manager.sh status
```

## 3. Test Connection

```bash
node test-browser-connection.js
```

## 4. View Logs

```bash
./browser-server-manager.sh logs
```

## 5. Stop Server

```bash
./browser-server-manager.sh stop
```

## Auto-Start Options

### Option A: Add to .bashrc
```bash
echo "source /mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/browser-server-persistent/auto-start.sh" >> ~/.bashrc
```

### Option B: Use Windows Task Scheduler
1. Open Task Scheduler
2. Create Basic Task
3. Trigger: When computer starts
4. Action: Start a program
5. Program: `C:\Windows\System32\wsl.exe`
6. Arguments: `-d Ubuntu -e /mnt/c/users/chad/source/repos/WitchCityRope/src/mcp-servers/browser-server-persistent/browser-server-manager.sh start`

### Option C: From Windows
Double-click `start-browser-server.bat`

## Troubleshooting

If server won't start:
1. Check Chrome path exists
2. Ensure port 9222 is free
3. Review error logs: `tail -f browser-server.error.log`