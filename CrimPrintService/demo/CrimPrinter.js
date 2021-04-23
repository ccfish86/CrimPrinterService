const PrintPool = new Map()

class CRIMPrinter {
  constructor(host = '127.0.0.1', port = '9999', reconnected, disconnected) {
    this.host = host
    this.port = port
    this.wsEnable = false
    this.reconnected = reconnected
    this.disconnected = disconnected
  }

  init({ host = this.host, port = this.port }, onReady, onError) {
    try {
      this.host = host
      this.port = port
      this.ws = new WebSocket('ws://' + host + ':' + port + '/Laputa')
      PrintPool.clear()
    } catch (ex) {
      // 新版本的IE、firefox等浏览器都允许通过websocket链接localhost  :(
      console.error('websock create failed', ex)
      if (onError) {
        onError('连接失败')
      }
      return
    }

    const _this = this
    this.ws.onopen = function(evt) {
      if (onReady) {
        onReady('连接成功')
      }
      this.wsOk = true
    }

    this.ws.onmessage = function() {
      var o = JSON.parse(arguments[0].data) // {\Seq\:\12221545\,\Data\:\end\}
      if (o) {
        console.log(o.Seq, o.Data)
        _this.processResult(o)
      }
      // this.ws.close()
    }
    // 错误处理
    this.ws.onerror = function(ev) {
      if (this.wsOk) {
        if (this.ws.readyState < 2) {
          //  alert('client.websock.onerror')
          //   this.reconn()
          onError('连接出错 state=' + this.ws.readyState)
        }
      } else {
        console.warn('websock create failed')
        if (onError) {
          onError('连接出错')
        }
      }
    }
    this.ws.onclose = function() {
      if (_this.disconnected && typeof _this.disconnected === 'function') {
        _this.disconnected('连接失败')
      }
      _this.reconnect()
    }
  }

  reconnect() {
    if (this.lockReconnect) {
      return
    }

    this.lockReconnect = true
    const _this = this
    setTimeout(function() {
      _this.init({ host: _this.host, port: _this.port }, () => {
        if (_this.reconnected && typeof _this.reconnected === 'function') {
          _this.reconnected('重连连接成功')
        }
      })
      console.log('正在重连，当前时间' + new Date())
      _this.lockReconnect = false
    }, 5000) // 这里设置重连间隔(ms)
  }

  print({ seq, url, printer }, callBack) {
    if (PrintPool.has(seq)) {
      callBack('重复打印')
      return // 异常
    }
    const req = {
      'Command': 'print',
      'Seq': seq,
      'Data': url,
      'Printer': printer
    }
    this.ws.send(JSON.stringify(req))
    PrintPool.set(seq, callBack)
    console.log('打印中:' + seq)
  }

  processResult(result) {
    if (result.Seq) {
      // {\Seq\:\12221545\,\Data\:\begin\}
      // {\Seq\:\12221545\,\Data\:\end\}
      // {\Seq\:\12221545\,\Data\:\printed\}
      if (PrintPool.has(result.Seq)) {
        console.log('result.Seq', result.Seq)
        const callBack = PrintPool.get(result.Seq)
        if (typeof callBack === 'function') {
          callBack(result)
        }

        if (result.Data === 'printed') {
          PrintPool.delete(result.Seq)
          console.log('PrintPool', PrintPool)
        }
      }
    }
  }
  //   reconn() {
  //     this.ws.
  //   }

  close() {
    if (this.ws) {
      this.ws.close()
    }
  }
}

const _CLIENT = new CRIMPrinter()

export default _CLIENT
