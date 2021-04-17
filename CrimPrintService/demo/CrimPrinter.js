const PrintPool = new Map()

class CRIMPrinter {
  constructor(host = '127.0.0.1', port = '9999') {
    this.host = host
    this.port = port
    this.wsEnable = false
  }

  init({ host = this.host, port = this.port }, onReady, onError) {
    try {
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
  }

  print(no, url, callBack) {
    if (PrintPool.has(no)) {
      return // 异常
    }
    const req = {
      'Command': 'print',
      'Seq': no,
      'Data': url
    }
    this.ws.send(JSON.stringify(req))
    PrintPool.set(no, callBack)
    console.log('打印中:' + no)
  }

  processResult(result) {
    if (result.Seq) {
      // {\Seq\:\12221545\,\Data\:\begin\}
      // {\Seq\:\12221545\,\Data\:\end\}
      // {\Seq\:\12221545\,\Data\:\printed\}
      if (PrintPool.has(result.Seq)) {
        const callBack = PrintPool.get(result.Seq)
        if (typeof callBack === 'function') {
          callBack(result)
        }

        if (result.Data === 'printed') {
          PrintPool.delete(result.Seq)
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
