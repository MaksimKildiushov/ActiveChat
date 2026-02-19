# GitHub Secrets — ActiveChat API

Перейди в репозиторий → Settings → Secrets and variables → Actions → New repository secret.

| Секрет              | Значение                                                | Обязательный |
|---------------------|---------------------------------------------------------|:------------:|
| `SSH_HOST`          | Внешний IP сервера (например `70.70.70.70`)             | ✅           |
| `SSH_USER`          | `root`                                                  | ✅           |
| `SSH_PRIVATE_KEY`   | Содержимое приватного SSH-ключа                         | ✅           |
| `SSH_PASSPHRASE`    | Passphrase от SSH-ключа (если ключ защищён)             | ✅           |
| `CR_USERNAME`       | Твой GitHub username                                    | ✅           |
| `CR_PAT`            | GitHub PAT с scope `read:packages`                      | ✅           |
| `APP_DOMAIN`        | Домен приложения (например `activechat.example.com`)    | ✅           |

> Traefik использует HTTP-01 challenge —
> работает с любым доменом и DNS-провайдером без дополнительных credentials.

---

## SSH_PRIVATE_KEY и SSH_PASSPHRASE

Вариант А — **существующий ключ с passphrase** (проще):

```bash
# Сгенерировать ключ специально для CI:
ssh-keygen -t ed25519 -C "github-actions" -f ~/.ssh/github_actions_activechat -N ""

# Добавить публичный ключ на сервер:
ssh-copy-id -i ~/.ssh/github_actions_activechat.pub root@SSH_HOST
```

# Содержимое приватного ключа → вставить в секрет SSH_PRIVATE_KEY:

Значение начинается с `-----BEGIN OPENSSH PRIVATE KEY-----`.

# Passphrase от этого ключа → в секрет SSH_PASSPHRASE

## CR_PAT

1. Перейди: https://github.com/settings/tokens/new → **Classic token**
2. Scope: `read:packages`
3. Скопировать значение в секрет `CR_PAT`

---

## Настройка DNS

Добавь A-запись у своего DNS-провайдера (серое облако / DNS only):

```
APP_DOMAIN  →  SSH_HOST
```
